/**
 * Copyright (C) 2015 crosire
 *
 * This software is  provided 'as-is', without any express  or implied  warranty. In no event will the
 * authors be held liable for any damages arising from the use of this software.
 * Permission  is granted  to anyone  to use  this software  for  any  purpose,  including  commercial
 * applications, and to alter it and redistribute it freely, subject to the following restrictions:
 *
 *   1. The origin of this software must not be misrepresented; you must not claim that you  wrote the
 *      original  software. If you use this  software  in a product, an  acknowledgment in the product
 *      documentation would be appreciated but is not required.
 *   2. Altered source versions must  be plainly  marked as such, and  must not be  misrepresented  as
 *      being the original software.
 *   3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.IO;
using System.Collections.Generic;

namespace Settings
{
	public sealed class ScriptSettings
	{
		#region Fields
		private string _fileName;
		private Dictionary<string, ValueAndComment> _values = new Dictionary<string, ValueAndComment>();
		#endregion

		struct ValueAndComment
		{
			public string SettingValue;
			public string Comment;

			public ValueAndComment(string value, string comment)
			{
				SettingValue = value;
				Comment = comment;
			}
		}

		private ScriptSettings(string fileName)
		{
			_fileName = fileName;
		}

		/// <summary>
		/// Loads a <see cref="ScriptSettings"/> from the specified file.
		/// </summary>
		/// <param name="filename">The filename to load the settings from.</param>
		public static ScriptSettings Load(string filename)
		{
			var result = new ScriptSettings(filename);

			if (!File.Exists(filename))
			{
				return result;
			}

			string line = null;
			string section = String.Empty;
			StreamReader reader = null;

			try
			{
				reader = new StreamReader(filename);
			}
			catch (IOException)
			{
				return result;
			}

			try
			{
				while (!ReferenceEquals(line = reader.ReadLine(), null))
				{
					line = line.Trim();

					if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("//"))
					{
						continue;
					}

					if (line.StartsWith("[") && line.Contains("]"))
					{
						section = line.Substring(1, line.IndexOf(']') - 1).Trim();
						continue;
					}
					else if (line.Contains("="))
					{
						int index = line.IndexOf('=');
						string key = line.Substring(0, index).Trim();
						string value = line.Substring(index + 1).Trim();

						if (value.Contains("//"))
						{
							value = value.Substring(0, value.IndexOf("//") - 1).TrimEnd();
						}
						if (value.StartsWith("\"") && value.EndsWith("\""))
						{
							value = value.Substring(1, value.Length - 2);
						}

						//string lookup = $"[{section}]{key}//0".ToUpper();
						string lookup = $"[{section}]{key}".ToUpper();

						if (result._values.ContainsKey(lookup))
						{
							//for (int i = 1; result._values.ContainsKey(lookup = $"[{section}]{key}//{i}".ToUpper()); ++i)
							for (int i = 1; result._values.ContainsKey(lookup = $"[{section}]{key}".ToUpper()); ++i)
							{
								continue;
							}
						}

						result._values.Add(lookup, new ValueAndComment(value, string.Empty));
					}
				}
			}
			finally
			{
				reader.Close();
			}

			return result;
		}

		/// <summary>
		/// Saves this <see cref="ScriptSettings"/> to file.
		/// </summary>
		/// <returns><c>true</c> if the file saved successfully; otherwise, <c>false</c></returns>
		public bool Save()
		{
			var result = new Dictionary<string, List<Tuple<string, ValueAndComment>>>();

			foreach (var data in _values)
			{
				string key = data.Key.Substring(data.Key.IndexOf("]") + 1);
				string section = data.Key.Remove(data.Key.IndexOf("]")).Substring(1);

				if (!result.ContainsKey(section))
				{
					var values = new List<Tuple<string, ValueAndComment>>();
					values.Add(new Tuple<string, ValueAndComment>(key, new ValueAndComment(data.Value.SettingValue, data.Value.Comment)));

					result.Add(section, values);
				}
				else
				{
					result[section].Add(new Tuple<string, ValueAndComment>(key, new ValueAndComment(data.Value.SettingValue, data.Value.Comment)));
				}
			}

			StreamWriter writer = null;

			try
			{
				writer = File.CreateText(_fileName);
			}
			catch (IOException)
			{
				return false;
			}

			try
			{
				foreach (var section in result)
				{
					writer.WriteLine("[" + section.Key + "]");

					foreach (var value in section.Value)
					{
						if (!string.IsNullOrWhiteSpace(value.Item2.Comment))
							writer.WriteLine(value.Item2.Comment);

						writer.WriteLine(value.Item1 + " = " + value.Item2.SettingValue);
					}

					writer.WriteLine();
				}
			}
			catch (IOException)
			{
				return false;
			}
			finally
			{
				writer.Close();
			}

			return true;
		}

		/// <summary>
		/// Reads a value from this <see cref="ScriptSettings"/>.
		/// </summary>
		/// <param name="section">The section where the value is.</param>
		/// <param name="name">The name of the key the value is saved at.</param>
		/// <param name="defaultvalue">The fall-back value if the key doesn't exist or casting to type <typeparamref name="T"/> fails.</param>
		/// <returns>The value at <see paramref="name"/> in <see paramref="section"/>.</returns>
		public T GetValue<T>(string section, string name, T defaultvalue)
		{
			//string lookup = $"[{section}]{name}//0".ToUpper();
			string lookup = $"[{section}]{name}".ToUpper();
			ValueAndComment internalValueAndComment;

			if (!_values.TryGetValue(lookup, out internalValueAndComment))
			{
				return defaultvalue;
			}

			try
			{
				var type = typeof(T);

				if (type.IsEnum)
				{
					return (T)(Enum.Parse(type, internalValueAndComment.SettingValue, true));
				}
				else
				{
					return (T)(Convert.ChangeType(internalValueAndComment.SettingValue, type));
				}
			}
			catch (Exception)
			{
				return defaultvalue;
			}
		}
		/// <summary>
		/// Sets a value in this <see cref="ScriptSettings"/>.
		/// </summary>
		/// <param name="section">The section where the value is.</param>
		/// <param name="name">The name of the key the value is saved at.</param>
		/// <param name="value">The value to set the key to.</param>
		/// <param name="comment">A comment. Must start with ; or //</param>
		public void SetValue<T>(string section, string name, T value, string comment = "")
		{
			//string lookup = $"[{section}]{name}//0".ToUpper();
			string lookup = $"[{section}]{name}".ToUpper();
			ValueAndComment internalValueAndComment = new ValueAndComment(value.ToString(), comment);

			if (!_values.ContainsKey(lookup))
			{
				_values.Add(lookup, internalValueAndComment);
			}

			_values[lookup] = internalValueAndComment;
		}

		/// <summary>
		/// Reads all the values at a specified key and section from this <see cref="ScriptSettings"/>.
		/// </summary>
		/// <param name="section">The section where the value is.</param>
		/// <param name="name">The name of the key the values are saved at.</param>
		public T[] GetAllValues<T>(string section, string name)
		{
			var values = new List<T>();
			ValueAndComment internalValueAndComment;

			//for (int i = 0; _values.TryGetValue($"[{section}]{name}//{i}".ToUpper(), out internalValueAndComment); ++i)
			for (int i = 0; _values.TryGetValue($"[{section}]{name}".ToUpper(), out internalValueAndComment); ++i)
			{
				try
				{
					if (typeof(T).IsEnum)
					{
						values.Add((T)(Enum.Parse(typeof(T), internalValueAndComment.SettingValue, true)));
					}
					else
					{
						values.Add((T)(Convert.ChangeType(internalValueAndComment.SettingValue, typeof(T))));
					}
				}
				catch
				{
					continue;
				}
			}

			return values.ToArray();
		}
	}
}