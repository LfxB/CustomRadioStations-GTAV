using GTA.Native;

/*
Modified from LucasRitter
https://github.com/LucasRitter
*/

namespace ScaleformHelper
{
    public class Scaleform
    {
        public int Handle { get; private set; }
        public string ScaleformID { get; private set; }

        public Scaleform(string scaleformId, bool forceLoad)
        {
            if (forceLoad)
                Load(scaleformId);
        }

        public Scaleform(int handle, bool forceLoad = false)
        {
            this.Handle = handle;
        }

        public bool Load(string scaleformId)
        {
            // Request a Scaleform movie identifier from the game.
            int handle = Function.Call<int>(Hash.REQUEST_SCALEFORM_MOVIE, scaleformId);

            // If a handle was not given, cancel the load process.
            if (handle == 0) return false;

            // Set local values.
            this.Handle = handle;
            this.ScaleformID = scaleformId;

            // Loading was successful, return true.
            return true;
        }

        public void Render2D()
        {
            // Draw a Scaleform movie in Fullscreen with 255 on all RGBA values.
            Function.Call(Hash.DRAW_SCALEFORM_MOVIE, this.Handle, 255, 255, 255, 255);
        }

        public void CallFunction (string function, params object[] arguments)
        {
            // Start calling the function.
            Function.Call(Hash._PUSH_SCALEFORM_MOVIE_FUNCTION, this.Handle, function);

            // Loop through all arguments.
            foreach (object argument in arguments)
            {
                // Do type checking

                // 32bit Integer
                if (argument.GetType() == typeof(int))
                {
                    // Call native GRAPHICS::_PUSH_SCALEFORM_MOVIE_METHOD_PARAMETER_INT
                    Function.Call(Hash._PUSH_SCALEFORM_MOVIE_FUNCTION_PARAMETER_INT, (int)argument);
                }

                else if (argument.GetType() == typeof(string))
                {
                    // Call native GRAPHICS::_PUSH_SCALEFORM_MOVIE_METHOD_PARAMETER_STRING
                    Function.Call(Hash._PUSH_SCALEFORM_MOVIE_FUNCTION_PARAMETER_STRING, (string)argument);
                }

            }

            // Pop the function over to the game.
            Function.Call(Hash._POP_SCALEFORM_MOVIE_FUNCTION_VOID);
        }

        public void CallFunctionArray (string function, params string[] arguments)
        {
            string[] argArray = new string[] { "", "", "", "", "" };

            for (int i = 0; i < 5; i++)
            {
                if (arguments.Length > i && arguments[i] != null)
                {
                    argArray[i] = arguments[i];
                }
            }

            // Call native GRAPHICS::_CALL_SCALEFORM_MOVIE_FUNCTION_STRING_PARAMS
            Function.Call(Hash._CALL_SCALEFORM_MOVIE_FUNCTION_STRING_PARAMS, this.Handle, function,
                argArray[0], argArray[1], argArray[2], argArray[3], argArray[4]);

            // Pop the function over to the game.
            Function.Call(Hash._POP_SCALEFORM_MOVIE_FUNCTION_VOID);
        }
    }
}