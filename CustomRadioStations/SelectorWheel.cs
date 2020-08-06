using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using GTA;
using GTA.Native;
using GTA.Math;
using Control = GTA.Control;
using Font = GTA.Font;

namespace SelectorWheel
{
    public delegate void CategoryChangeEvent(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem, bool wheelJustOpened);
    public delegate void ItemChangeEvent(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem, bool wheelJustOpened, GoTo direction);
    public delegate void WheelOpenEvent(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem);
    public delegate void WheelCloseEvent(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem);
    public delegate void WheelItemTrigger(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem);

    public enum GoTo
    {
        Prev,
        Next,
        Same
    }

    public class Wheel
    {
        public string WheelName { get; set; }
        private bool _visible;
        public int CurrentCatIndex = 0;
        public List<WheelCategory> Categories = new List<WheelCategory>();

        Vector2 _origin = new Vector2(0.5f, 0.4f);
        Vector2 inputCoord = Vector2.Zero;
        public float Radius = 250;
        //float inputAngle =  265f;
        const float controllerDeadzone = 0.005f;
        const float keyboardDeadzone = 0.03f;

        bool UseTextures;
        bool HaveTexturesBeenCached;
        string TexturePath;
        int TextureRefreshRate;
        int xTextureOffset = 0;
        int yTextureOffset = 0;

        string TextureCatBgPath = ""; // Background path
        Color TextureCatBgColor;
        double TextureCatBgSizeMultiple;
        string TextureCatHlPath = ""; // Highlight path
        Color TextureCatBgHighlightColor;
        double TextureCatBgHighlightSizeMultiple;

        private Size _textureSize;
        public Size TextureSize
        {
            get { return _textureSize; }
            set
            {
                _textureSize = new Size((int)(value.Width * (16f / 9f)), value.Height);
            }
        }

        static bool transitionIn;
        static bool transitionOut;

        static float timeScale = 1f;

        /// <summary>
        /// https://pastebin.com/kVPwMemE
        /// </summary>
        public static string TimecycleModifier = "hud_def_desat_Neutral";
        public static float TimecycleModifierStrength = 1.0f;
        static float timecycleCurrentStrength = 0f;

        const string AUDIO_SOUNDSET = "HUD_FRONTEND_DEFAULT_SOUNDSET";
        const string AUDIO_SELECTSOUND = "HIGHLIGHT_NAV_UP_DOWN";

        const string QuickMutedAudioScene = "FADE_OUT_WORLD_250MS_SCENE";
        const string MutedMuffledAudioScene = "DEATH_SCENE";

        /// <summary>
        /// Called when user hovers over a new category.
        /// </summary>
        public event CategoryChangeEvent OnCategoryChange;

        /// <summary>
        /// Called when user switches to a new item.
        /// </summary>
        public event ItemChangeEvent OnItemChange;

        /// <summary>
        /// Called when user opens the wheel.
        /// </summary>
        public event WheelOpenEvent OnWheelOpen;

        /// <summary>
        /// Called when user closes the wheel.
        /// </summary>
        public event WheelCloseEvent OnWheelClose;

        /// <summary>
        /// Called when the TriggerSelectedItem() method is called.
        /// An external class has to call it itself.
        /// </summary>
        public event WheelItemTrigger OnItemTrigger;

        /// <summary>
        /// Show/Hide the selection wheel.
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
            set
            {
                //start and end screen effects, etc. before toggling.

                if (_visible == false && value == true) //When the wheel is just opened.
                {
                    Function.Call(Hash.SET_TIMECYCLE_MODIFIER, TimecycleModifier);
                    Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, timecycleCurrentStrength);
                    transitionIn = true;
                    transitionOut = false;

                    CalculateCategoryPlacement();
                    UIHelper.UpdateAspectRatio();

                    CategoryChange(SelectedCategory, SelectedCategory.SelectedItem, true);
                    ItemChange(SelectedCategory, SelectedCategory.SelectedItem, true, GoTo.Same);
                    WheelOpen(SelectedCategory, SelectedCategory.SelectedItem);
                }
                else if (_visible == true && value == false) //When the wheel is just closed.
                {
                    transitionIn = false;
                    transitionOut = true;

                    WheelClose(SelectedCategory, SelectedCategory.SelectedItem);
                    foreach (var cat in Categories)
                    {
                        if (cat.CategoryTexture != null)
                        {
                            cat.CategoryTexture.StopDraw();
                        }
                        if (cat.BackgroundTexture != null)
                        {
                            cat.BackgroundTexture.StopDraw();
                        }
                        if (cat.HighlightTexture != null)
                        {
                            cat.HighlightTexture.StopDraw();
                        }
                        if (cat.SelectedItem.ItemTexture != null)
                        {
                            cat.SelectedItem.ItemTexture.StopDraw();
                        }
                    }
                }

                _visible = value;
            }
        }

        /// <summary>
        /// Instantiates a simple Selection Wheel that does not use textures. Just displays the category and item names.
        /// </summary>
        /// <param name="name">Name of the wheel. I was planning to display it while the wheel is shown but I didn't implement it yet.</param>
        /// <param name="wheelRadius">Length from the origin.</param>
        public Wheel(string name, float wheelRadius = 250)
        {
            WheelName = name;
            Radius = wheelRadius;
        }

        /// <summary>
        /// Instantiates a Selection Wheel that uses textures for categories or items, if they exist.
        /// </summary>
        /// <param name="name">Name of the wheel. I was planning to display it while the wheel is shown but I didn't implement it yet.</param>
        /// <param name="texturePath">Path where category and item .png files are kept. Ex: @"scripts\SelectorWheelExample\"</param>
        /// <param name="xtextureOffset">Simple X offset, usually set to 0.</param>
        /// <param name="ytextureOffset">Simple Y offset, usually set to 0.</param>
        /// <param name="textureSize">Size of images (in pixels).</param>
        /// <param name="textureRefreshRate">How long (in ms) each texture will be displayed for.</param>
        /// <param name="wheelRadius">Length from the origin.</param>
        public Wheel(string name, string texturePath, int xtextureOffset, int ytextureOffset, Size textureSize, int textureRefreshRate = 50, float wheelRadius = 250)
        {
            WheelName = name;
            UseTextures = true;
            TexturePath = texturePath;
            xTextureOffset = xtextureOffset;
            yTextureOffset = ytextureOffset;
            TextureSize = textureSize;
            TextureRefreshRate = textureRefreshRate;
            Radius = wheelRadius;
        }

        /// <summary>
        /// Must be placed in your Tick method.
        /// </summary>
        public void ProcessSelectorWheel()
        {
            // Now needs to be called alongside ProcessSelectorWheel().
            //ControlTransitions();
            if (!Visible) return;

            DisableControls();
            ControlCategorySelection();
            ControlItemSelection();
        }

        public static void ControlTransitions(bool useSlowmotion)
        {
            if (transitionIn)
            {
                if (!Function.Call<bool>(Hash.IS_AUDIO_SCENE_ACTIVE, MutedMuffledAudioScene))
                {
                    Function.Call(Hash.START_AUDIO_SCENE, QuickMutedAudioScene);
                    Function.Call(Hash.SET_AUDIO_SCENE_VARIABLE, QuickMutedAudioScene, "apply", 0.8f);
                    Function.Call(Hash.START_AUDIO_SCENE, MutedMuffledAudioScene);
                }

                float amount = Game.LastFrameTime * 8f;

                if (useSlowmotion)
                {
                    float tempTScale = DecreaseNum(timeScale, amount, 0.05f);
                    Game.TimeScale = tempTScale;
                    timeScale = tempTScale;
                }
                else
                {
                    timeScale = 1;
                    Game.TimeScale = 1;
                }

                float tempStrength = IncreaseNum(timecycleCurrentStrength, amount, TimecycleModifierStrength);
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, tempStrength);
                timecycleCurrentStrength = tempStrength;
            }
            if (transitionOut)
            {
                if (Function.Call<bool>(Hash.IS_AUDIO_SCENE_ACTIVE, MutedMuffledAudioScene))
                {
                    Function.Call(Hash.STOP_AUDIO_SCENE, MutedMuffledAudioScene);
                    Function.Call(Hash.STOP_AUDIO_SCENE, QuickMutedAudioScene);
                }

                float amount = Game.LastFrameTime * 8f;

                if (useSlowmotion)
                {
                    float tempTScale = IncreaseNum(timeScale, amount, 1f);
                    Game.TimeScale = tempTScale;
                    timeScale = tempTScale;
                }
                else
                {
                    timeScale = 1;
                    Game.TimeScale = 1;
                }

                float tempStrength = DecreaseNum(timecycleCurrentStrength, Game.LastFrameTime * 2f, 0f);
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, tempStrength);
                timecycleCurrentStrength = tempStrength;
                if (timecycleCurrentStrength <= 0f && timeScale >= 1f)
                {
                    Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
                    transitionOut = false;
                }
            }

        }

        /// <summary>
        /// Call this after you have already added some categories (max is 18 categories. Edit: Limit removed temporarily).
        /// This function will check the amount of categories and situate them around the origin of the screen.
        /// 
        /// 
        /// </summary>
        public void CalculateCategoryPlacement()
        {
            /* 0f is on the middle-right, and it moves clockwise.
             * i.e. 270f is directly up.
             * */

            /*switch (Categories.Count)
            {
                case 1:
                    {
                        //Categories[0].position2D = PointOnCircleInPercentage(Radius, 270f, OriginInPixels);
                        CalculateFromStartAngle(270f, 1);
                        break;
                    }
                case 4:
                    {
                        CalculateFromStartAngle(225f, 4);
                        break;
                    }
                default:
                    {
                        CalculateFromStartAngle(270f, Categories.Count);
                        break;
                    }
            }*/

            CalculateFromStartAngle(270f, Categories.Count);

            if (!HaveTexturesBeenCached)
            {
                foreach (var cat in Categories)
                {
                    bool hasTexture = false;
                    if (File.Exists(Path.Combine(TexturePath, UIHelper.MakeValidFileName(cat.Name) + ".png")))
                    {
                        cat.CategoryTexture = new Texture(Path.Combine(TexturePath,UIHelper.MakeValidFileName(cat.Name) + ".png"), Categories.IndexOf(cat));
                        hasTexture = true;
                    }
                    foreach (var item in cat.ItemList)
                    {
                        if (File.Exists(Path.Combine(TexturePath, UIHelper.MakeValidFileName(item.Name) + ".png")))
                        {
                            item.ItemTexture = new Texture(Path.Combine(TexturePath, UIHelper.MakeValidFileName(item.Name) + ".png"), Categories.IndexOf(cat) /*cat.ItemList.IndexOf(item)*/);
                            hasTexture = true;
                        }
                    }

                    if (hasTexture)
                    {
                        if (!string.IsNullOrWhiteSpace(TextureCatBgPath))
                        {
                            cat.BackgroundTexture = new Texture(TextureCatBgPath, Categories.IndexOf(cat) + Categories.Count);
                        }
                        if (!string.IsNullOrWhiteSpace(TextureCatHlPath))
                        {
                            cat.HighlightTexture = new Texture(TextureCatHlPath, Categories.IndexOf(cat) + (Categories.Count * 2));
                        }
                    }

                    /*Load textures into cache*/
                    if (cat.CategoryTexture != null)
                    {
                        cat.CategoryTexture.LoadTexture();
                    }
                    if (cat.BackgroundTexture != null)
                    {
                        cat.BackgroundTexture.LoadTexture();
                    }
                    if (cat.HighlightTexture != null)
                    {
                        cat.HighlightTexture.LoadTexture();
                    }
                    foreach (var item in cat.ItemList)
                    {
                        if (item.ItemTexture != null)
                        {
                            item.ItemTexture.LoadTexture();
                        }
                    }
                }

                HaveTexturesBeenCached = true;
            }
        }

        void CalculateFromStartAngle(float startAngle, int numCategories)
        {
            if (numCategories < 1) return;
            float angleOffset = 360 / numCategories;
            for (int i = 0; i < numCategories; i++)
            {
                Categories[i].position2D = PointOnCircleInPercentage(Radius, startAngle, OriginInPixels);
                startAngle += angleOffset;
            }
        }

        /// <summary>
        /// In screen percentage.
        /// X: 0.5f = 50% from the left.
        /// Y: 0.5f = 50% from the top.
        /// Set this before calling CalculateCategoryPlacement() or it won't apply.
        /// </summary>
        public Vector2 Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }

        public Vector2 OriginInPixels
        {
            get { return new Vector2(UIHelper.XPercentageToPixel(_origin.X), UIHelper.YPercentageToPixel(_origin.Y)); }
        }

        private float AddXPixelDistanceToPercent(float percent, int pixelDist)
        {
            return UIHelper.XPixelToPercentage
                (
                    (int)UIHelper.XPercentageToPixel(percent) + pixelDist
                );
        }

        private float AddYPixelDistanceToPercent(float percent, int pixelDist)
        {
            return UIHelper.YPixelToPercentage
                (
                    (int)UIHelper.YPercentageToPixel(percent) + pixelDist
                );
        }

        /// <summary>
        /// Taken from https://stackoverflow.com/a/839904
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="angleInDegrees"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private static Vector2 PointOnCircleInPercentage(float radius, float angleInDegrees, Vector2 origin)
        {
            // Convert from degrees to radians via multiplication by PI/180   
            double radians = angleInDegrees * Math.PI / 180F;
            float x = (float)(radius * Math.Cos(radians)) + origin.X;
            float y = (float)(radius * Math.Sin(radians)) + origin.Y;

            return new Vector2(UIHelper.XPixelToPercentage((int)x), UIHelper.YPixelToPercentage((int)y));
        }

        private static Size SizeMultiply(Size size, double factor)
        {
            return new Size((int)(size.Width * factor), (int)(size.Height * factor));
        }

        private float CalculateRelativeValue(float input, float inputMin, float inputMax, float outputMin, float outputMax)
        {
            //http://stackoverflow.com/questions/22083199/method-for-calculating-a-value-relative-to-min-max-values
            //Making sure bounderies arent broken...
            if (input > inputMax)
            {
                input = inputMax;
            }
            if (input < inputMin)
            {
                input = inputMin;
            }
            //Return value in relation to min og max

            double position = (double)(input - inputMin) / (inputMax - inputMin);

            float relativeValue = (float)(position * (outputMax - outputMin)) + outputMin;

            return relativeValue;
        }

        private static float IncreaseNum(float num, float increment, float max)
        {
            return num + increment > max ? max : num + increment;
        }

        private static float DecreaseNum(float num, float decrement, float min)
        {
            return num - decrement < min ? min : num - decrement;
        }

        /// <summary>
        /// Default is an empty string. 
        /// Setting a proper path will show the targetted .png image behind each category icon. 
        /// Call before <see cref="CalculateCategoryPlacement"/>
        /// </summary>
        /// <param name="pathBg"></param>
        public void SetCategoryBackgroundIcons(string pathBg, Color bgColor, double bgSizeMultiple, string pathHl, Color highlightColor, double hlSizeMultiple)
        {
            bool exists = File.Exists(pathBg);

            TextureCatBgPath = exists ? pathBg : "";
            TextureCatBgColor = bgColor;
            TextureCatBgSizeMultiple = bgSizeMultiple;

            exists = File.Exists(pathHl);
            TextureCatHlPath = exists ? pathHl : "";
            TextureCatBgHighlightColor = highlightColor;
            TextureCatBgHighlightSizeMultiple = hlSizeMultiple;
        }

        /// <summary>
        /// Add category to this wheel.
        /// </summary>
        /// <param name="category"></param>
        public void AddCategory(WheelCategory category)
        {
            //if (Categories.Count == 18) return; //Don't allow more than 18 categories.

            Categories.Add(category);
        }

        public void ClearAllCategories()
        {
            Categories.Clear();
        }

        public void RemoveCategory(WheelCategory cat)
        {
            Categories.Remove(cat);
        }

        public bool IsCategorySelected(WheelCategory cat)
        {
            if (Categories.Contains(cat))
            {
                return Categories.IndexOf(cat) == CurrentCatIndex;
            }
            return false;
        }

        public WheelCategory SelectedCategory
        {
            get
            {
                return Categories[CurrentCatIndex];
            }
            set
            {
                /*if (Categories.Exists(x => x.Name.Equals(value.Name)
                    && x.Description.Equals(value.Description)))
                {
                    CurrentCatIndex = Categories.FindIndex(x => x.Name.Equals(value.Name)
                        && x.Description.Equals(value.Description));
                }*/
                
                if (Categories.Exists(x => x.Equals(value)))
                {
                    CurrentCatIndex = Categories.FindIndex(x => x.Equals(value));
                    inputCoord = Categories[CurrentCatIndex].position2D;
                }
            }
        }

        public Font FontCategory = Font.ChaletComprimeCologne;
        public Font FontSelectedItem = Font.ChaletComprimeCologne;
        public Font FontCategoryItemCount = Font.ChaletComprimeCologne;
        public Font FontDescription = Font.ChaletLondon;
        void ControlCategorySelection()
        {
            foreach (var cat in Categories)
            {
                bool isSelectedCategory = SelectedCategory == cat;

                bool catTextureExists = cat.CategoryTexture != null; //File.Exists(TexturePath + UIHelper.MakeValidFileName(cat.Name) + ".png");
                bool itemTextureExists = cat.SelectedItem.ItemTexture != null; //File.Exists(TexturePath + UIHelper.MakeValidFileName(cat.SelectedItem.Name) + ".png");
                bool bgTextureExists = cat.BackgroundTexture != null;
                bool hlTextureExists = cat.HighlightTexture != null;
                bool anyTextureExists = catTextureExists || itemTextureExists;

                if (UseTextures && anyTextureExists)
                {
                    Texture temp = catTextureExists ? cat.CategoryTexture : cat.SelectedItem.ItemTexture;
                    temp.Draw(3, TextureRefreshRate,
                        new Point((int)(cat.position2D.X * UI.WIDTH) + xTextureOffset, (int)(cat.position2D.Y * UI.HEIGHT) + yTextureOffset),
                        new PointF(0.5f, 0.5f),
                        isSelectedCategory && !bgTextureExists ? SizeMultiply(TextureSize, 1.25) : TextureSize,
                        0f, isSelectedCategory ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(120, 255, 255, 255), UIHelper.AspectRatio);

                    if (bgTextureExists)
                    {
                        cat.BackgroundTexture.Draw(2, TextureRefreshRate,
                            new Point((int)(cat.position2D.X * UI.WIDTH) + xTextureOffset, (int)(cat.position2D.Y * UI.HEIGHT) + yTextureOffset),
                            new PointF(0.5f, 0.5f),
                            SizeMultiply(TextureSize, TextureCatBgSizeMultiple),
                            0f, isSelectedCategory ? TextureCatBgColor : Color.FromArgb(120, TextureCatBgColor.R, TextureCatBgColor.G, TextureCatBgColor.B), UIHelper.AspectRatio);

                    }
                    if (isSelectedCategory && hlTextureExists)
                    {
                        cat.HighlightTexture.Draw(1, TextureRefreshRate,
                            new Point((int)(cat.position2D.X * UI.WIDTH) + xTextureOffset, (int)(cat.position2D.Y * UI.HEIGHT) + yTextureOffset),
                            new PointF(0.5f, 0.5f),
                            SizeMultiply(TextureSize, TextureCatBgHighlightSizeMultiple),
                            0f, TextureCatBgHighlightColor, UIHelper.AspectRatio);
                    }
                }
                else
                {
                    Color col = isSelectedCategory ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(120, 255, 255, 255);
                    UIHelper.DrawCustomText(cat.Name, 0.8f, FontCategory, col.R, col.G, col.B, col.A, cat.position2D.X, cat.position2D.Y, 50, 0, 0, 0, 255, UIHelper.TextJustification.Center);
                }

            }

            UIHelper.DrawCustomText(SelectedCategory.SelectedItem.Name, 0.45f, FontSelectedItem, 255, 255, 255, 255, _origin.X, AddYPixelDistanceToPercent(_origin.Y, -50), 50, 0, 0, 0, 255, UIHelper.TextJustification.Center);
            if (SelectedCategory.ItemCount() > 1)
            {
                UIHelper.DrawCustomText((SelectedCategory.CurrentItemIndex + 1).ToString() + " / " + SelectedCategory.ItemCount().ToString(), 0.55f, FontCategoryItemCount, 255, 255, 255, 255, _origin.X, AddYPixelDistanceToPercent(_origin.Y, -50), 50, 0, 0, 0, 255, UIHelper.TextJustification.Center);
            }

            if (SelectedCategory.SelectedItem.Description != null)
            {
                float pixelX = 964f / (float)UI.WIDTH;
                float pixelY = 100f / (float)UI.HEIGHT;
                UIHelper.DrawCustomText(SelectedCategory.SelectedItem.Description, 0.35f, FontDescription, 255, 255, 255, 255, pixelX, pixelY, 0, 0, 0, 0, 0, UIHelper.TextJustification.Left, true, pixelX, 1250f / (float)UI.WIDTH, true, 0, 0, 0, 180, 10f / (float)UI.WIDTH, 10f / (float)UI.HEIGHT);
            }
            else if (SelectedCategory.Description != null)
            {
                float pixelX = 964f / (float)UI.WIDTH;
                float pixelY = 100f / (float)UI.HEIGHT;
                UIHelper.DrawCustomText(SelectedCategory.Description, 0.35f, FontDescription, 255, 255, 255, 255, pixelX, pixelY, 0, 0, 0, 0, 0, UIHelper.TextJustification.Left, true, pixelX, 1250f / (float)UI.WIDTH, true, 0, 0, 0, 180, 10f / (float)UI.WIDTH, 10f / (float)UI.HEIGHT);
            }

            CategorySelectionControls();
        }

        DateTime inputTimer = DateTime.Now;
        private void CategorySelectionControls()
        {
            if (new Vector2(WheelLeftRightValue(), WheelUpDownValue()).Length() > controllerDeadzone)
            {
                //inputAngle = InputToAngle();
                inputCoord = PointOnCircleInPercentage(Radius, InputToAngle(), OriginInPixels);
            }

            /*UIHelper.DrawRectangle(inputCoord.X, inputCoord.Y, 0.05f, 0.05f, 0, 235, 255, 255);
            UI.ShowSubtitle(Math.Round(new Vector2(WheelLeftRightValue(), WheelUpDownValue()).Length(), 2).ToString());*/

            int inputIndex = ClosestCategoryToInputCoord() != null ? Categories.IndexOf(ClosestCategoryToInputCoord()) : CurrentCatIndex;

            //int nextClosest = NextClosestIndexWithWrap(Categories, CurrentCatIndex, inputIndex);
            if (inputIndex != CurrentCatIndex /*&& nextClosest != CurrentCatIndex*/)
            {
                //if (Game.CurrentInputMode == InputMode.GamePad)
                //{
                //// Stop cat bg and highligh draw
                if (!string.IsNullOrWhiteSpace(TextureCatHlPath))
                {
                    //Categories[CurrentCatIndex].BackgroundTexture.StopDraw();
                    var temp = Categories[CurrentCatIndex];
                    if (temp.HighlightTexture != null)
                        temp.HighlightTexture.StopDraw();
                }
                CurrentCatIndex = inputIndex;
                //}
                //else
                //{
                //    CurrentCatIndex = nextClosest;
                //}

                CategoryChange(SelectedCategory, SelectedCategory.SelectedItem, false);
                ItemChange(SelectedCategory, SelectedCategory.SelectedItem, false, GoTo.Same);
                Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
            }

            /*if (Game.CurrentInputMode == InputMode.GamePad)
            {
                if (inputIndex != CurrentCatIndex)
                {
                    CurrentCatIndex = inputIndex;

                    CategoryChange(SelectedCategory, SelectedCategory.SelectedItem, false);
                    ItemChange(SelectedCategory, SelectedCategory.SelectedItem, false);
                    Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
                }
            }
            else
            {
                UI.ShowSubtitle("UpDown: " + WheelUpDownValue() + "\nLeftRight: " + WheelLeftRightValue());
                if (inputTimer < DateTime.Now)
                {
                    WheelDirection dir = GetMouseDirection();

                    if (dir == WheelDirection.NotMoving) return;

                    if (CurrentCatIndex == inputIndex) return;

                    bool onLeft = IndexIsWithinCategoryPercentRange(CurrentCatIndex, 0.5f, 1f);
                    bool onRight = IndexIsWithinCategoryPercentRange(CurrentCatIndex, 0f, 0.5f);
                    bool onTop = IndexIsWithinCategoryPercentRange(CurrentCatIndex, 0f, 0.25f) || IndexIsWithinCategoryPercentRange(CurrentCatIndex, 0.75f, 1f);
                    bool onBottom = IndexIsWithinCategoryPercentRange(CurrentCatIndex, 0.25f, 0.75f);
                    int tempIndex = 0;
                    bool changeCatIndex = false;
                    if (dir == WheelDirection.MovingDown)
                    {
                        if (onLeft)
                        {
                            tempIndex = GetDecreasedIndex();
                            if (IndexIsWithinCategoryPercentRange(tempIndex, 0.5f, 1f))
                                changeCatIndex = true;
                        }
                        else if (onRight)
                        {
                            tempIndex = GetIncreasedIndex();
                            if (IndexIsWithinCategoryPercentRange(tempIndex, 0f, 0.5f))
                                changeCatIndex = true;
                        }
                    }
                    else if (dir == WheelDirection.MovingUp)
                    {
                        if (onLeft)
                        {
                            tempIndex = GetIncreasedIndex();
                            if (IndexIsWithinCategoryPercentRange(tempIndex, 0.5f, 1f))
                                changeCatIndex = true;
                        }
                        else if (onRight)
                        {
                            tempIndex = GetDecreasedIndex();
                            if (IndexIsWithinCategoryPercentRange(tempIndex, 0f, 0.5f))
                                changeCatIndex = true;
                        }
                    }
                    else if (dir == WheelDirection.MovingRight)
                    {
                        if (onTop)
                        {
                            tempIndex = GetIncreasedIndex();
                            if (IndexIsWithinCategoryPercentRange(tempIndex, 0f, 0.25f) || IndexIsWithinCategoryPercentRange(tempIndex, 0.75f, 1f))
                                changeCatIndex = true;
                        }
                        else if (onBottom)
                        {
                            tempIndex = GetDecreasedIndex();
                            if (IndexIsWithinCategoryPercentRange(tempIndex, 0.25f, 0.75f))
                                changeCatIndex = true;
                        }
                    }
                    else if (dir == WheelDirection.MovingLeft)
                    {
                        if (onTop)
                        {
                            tempIndex = GetDecreasedIndex();
                            if (IndexIsWithinCategoryPercentRange(tempIndex, 0f, 0.25f) || IndexIsWithinCategoryPercentRange(tempIndex, 0.75f, 1f))
                                changeCatIndex = true;
                        }
                        else if (onBottom)
                        {
                            tempIndex = GetIncreasedIndex();
                            if (IndexIsWithinCategoryPercentRange(tempIndex, 0.25f, 0.75f))
                                changeCatIndex = true;
                        }
                    }

                    //inputTimer = DateTime.Now.AddMilliseconds(
                    //Math.Min(400, (int)(20 / Math.Abs(WheelUpDownValue())))
                    //);
                    inputTimer = DateTime.Now.AddMilliseconds(50);

                    if (!changeCatIndex) return;
                    CurrentCatIndex = tempIndex;
                    CategoryChange(SelectedCategory, SelectedCategory.SelectedItem, false);
                    ItemChange(SelectedCategory, SelectedCategory.SelectedItem, false);
                    Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
                }
            }*/
        }

        public static int NextClosestIndexWithWrap<T>(List<T> list, int currIndex, int toIndex)
        {
            if (currIndex == toIndex) return currIndex;
            if (currIndex >= list.Count || toIndex >= list.Count) return 0;
            int dist = toIndex > currIndex ? toIndex - currIndex : currIndex - toIndex;
            int distThroughZero = toIndex < currIndex ? list.Count - currIndex + toIndex : list.Count - toIndex + currIndex;
            if (distThroughZero < dist)
            {
                if (toIndex < currIndex)
                {
                    return currIndex == list.Count - 1 ? 0 : Math.Min(currIndex + 1, list.Count);
                }
                else
                {
                    return currIndex == 0 ? list.Count - 1 : Math.Max(0, currIndex - 1);
                }
            }
            else
            {
                if (toIndex < currIndex)
                {
                    return currIndex - 1;
                }
                else
                {
                    return currIndex + 1;
                }
            }
        }

        enum WheelDirection
        {
            MovingUp,
            MovingDown,
            MovingLeft,
            MovingRight,
            NotMoving
        }

        private WheelDirection GetMouseDirection()
        {
            var ud = WheelUpDownValue();
            var lr = WheelLeftRightValue();

            if (Math.Abs(ud) < keyboardDeadzone && Math.Abs(lr) < keyboardDeadzone) return WheelDirection.NotMoving;

            if (Math.Abs(ud) > Math.Abs(lr))
            {
                return ud > 0f ? WheelDirection.MovingDown : WheelDirection.MovingUp;
            }
            else
            {
                return lr > 0f ? WheelDirection.MovingRight : WheelDirection.MovingLeft;
            }
        }

        private int GetIncreasedIndex(bool wrap = true)
        {
            int temp = CurrentCatIndex;
            if (temp < Categories.Count - 1)
            {
                temp++;
            }
            else
            {
                if (wrap) temp = 0;
            }
            return temp;
        }

        private int GetDecreasedIndex(bool wrap = true)
        {
            int temp = CurrentCatIndex;
            if (temp > 0)
            {
                temp--;
            }
            else
            {
                if (wrap) temp = Categories.Count - 1;
            }
            return temp;
        }

        private bool IndexIsWithinCategoryPercentRange(int index, float startInclusive, float endInclusive)
        {
            float percentage = index / (float)Categories.Count;
            if (endInclusive == 1f && index == 0) return true;
            return percentage >= startInclusive && percentage <= endInclusive ? true : false;
        }

        float InputToAngle()
        {
            var angle = Math.Atan2(Game.GetControlNormal(2, GTA.Control.WeaponWheelUpDown), Game.GetControlNormal(2, GTA.Control.WeaponWheelLeftRight));
            if (angle < 0)
            {
                angle += Math.PI * 2;
            }
            return (float)(angle * (180 / Math.PI));
        }

        static double GetDistance(Vector2 point1, Vector2 point2)
        {
            //pythagorean theorem c^2 = a^2 + b^2
            //thus c = square root(a^2 + b^2)
            double a = (double)(point2.X - point1.X);
            double b = (double)(point2.Y - point1.Y);

            return Math.Sqrt(a * a + b * b);
        }

        WheelCategory ClosestCategoryToInputCoord()
        {
            return Categories.OrderBy(c => GetDistance(c.position2D, inputCoord)).First();
        }

        public void TriggerSelectedItemEvent()
        {
            if (SelectedCategory == null || Categories.Count < 1 || SelectedCategory.SelectedItem == null)
                return;

            ItemTrigger(SelectedCategory, SelectedCategory.SelectedItem);
        }

        void ControlItemSelection()
        {
            if (Control_GoToNextItemInCategory_Pressed())
            {
                if (SelectedCategory.SelectedItem.ItemTexture != null && SelectedCategory.ItemCount() > 1) { SelectedCategory.SelectedItem.ItemTexture.StopDraw(); }
                SelectedCategory.GoToNextItem();
                ItemChange(SelectedCategory, SelectedCategory.SelectedItem, false, GoTo.Next);
                Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
            }
            else if (Control_GoToPreviousItemInCategory_Pressed())
            {
                if (SelectedCategory.SelectedItem.ItemTexture != null && SelectedCategory.ItemCount() > 1) { SelectedCategory.SelectedItem.ItemTexture.StopDraw(); }
                SelectedCategory.GoToPreviousItem();
                ItemChange(SelectedCategory, SelectedCategory.SelectedItem, false, GoTo.Prev);
                Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
            }
        }

        List<Control> ControlsToEnable = new List<Control>
            {
                /*Control.FrontendAccept,
                Control.FrontendAxisX,
                Control.FrontendAxisY,
                Control.FrontendDown,
                Control.FrontendUp,
                Control.FrontendLeft,
                Control.FrontendRight,
                Control.FrontendCancel,
                Control.FrontendSelect,
                Control.CharacterWheel,
                Control.CursorScrollDown,
                Control.CursorScrollUp,
                Control.CursorX,
                Control.CursorY,*/
                Control.MoveUpDown,
                Control.MoveLeftRight,
                Control.Sprint,
                Control.Jump,
                Control.Enter,
                Control.VehicleExit,
                Control.VehicleAccelerate,
                Control.VehicleBrake,
                Control.VehicleMoveLeftRight,
                Control.VehicleFlyYawLeft,
                Control.FlyLeftRight,
                Control.FlyUpDown,
                Control.VehicleFlyYawRight,
                Control.VehicleHandbrake,
                Control.WeaponWheelLeftRight,
                Control.WeaponWheelUpDown,
                //Control.VehicleCinematicLeftRight,
                //Control.VehicleCinematicUpDown
                /*Control.VehicleRadioWheel,
                Control.VehicleRoof,
                Control.VehicleHeadlight,
                Control.VehicleCinCam,
                Control.Phone,
                Control.MeleeAttack1,
                Control.MeleeAttack2,
                Control.Attack,
                Control.Attack2
                Control.LookUpDown,
                Control.LookLeftRight*/
            };

        protected void DisableControls()
        {
            Game.DisableAllControlsThisFrame(2);

            foreach (var con in ControlsToEnable)
            {
                Game.EnableControlThisFrame(2, con);
            }
        }

        /// <summary>
        /// Right: positive 1
        /// Left: negative 1
        /// </summary>
        /// <returns>normalized value of left/right mouse/stick movement.</returns>
        float WheelLeftRightValue()
        {
            return Game.GetControlNormal(2, Control.WeaponWheelLeftRight);
        }
        
        /// <summary>
        /// Down: positive 1
        /// Up: negative 1
        /// </summary>
        /// <returns>normalized value of up/down mouse/stick movement.</returns>
        float WheelUpDownValue()
        {
            return Game.GetControlNormal(2, Control.WeaponWheelUpDown);
        }

        bool Control_GoToNextItemInCategory_Pressed()
        {
            return Game.IsControlJustPressed(2, Game.CurrentInputMode == InputMode.MouseAndKeyboard ?
                Control.WeaponWheelPrev : Control.VehicleAccelerate);
        }

        bool Control_GoToPreviousItemInCategory_Pressed()
        {
            return Game.IsControlJustPressed(2, Game.CurrentInputMode == InputMode.MouseAndKeyboard ? 
                Control.WeaponWheelNext : Control.VehicleBrake);
        }
        
        protected virtual void CategoryChange(WheelCategory selectedCategory, WheelCategoryItem selecteditem, bool wheelJustOpened)
        {
            OnCategoryChange?.Invoke(this, selectedCategory, selecteditem, wheelJustOpened);
        }

        protected virtual void ItemChange(WheelCategory selectedCategory, WheelCategoryItem selecteditem, bool wheelJustOpened, GoTo direction)
        {
            OnItemChange?.Invoke(this, selectedCategory, selecteditem, wheelJustOpened, direction);
        }

        protected virtual void WheelOpen(WheelCategory selectedCategory, WheelCategoryItem selecteditem)
        {
            OnWheelOpen?.Invoke(this, selectedCategory, selecteditem);
        }

        protected virtual void WheelClose(WheelCategory selectedCategory, WheelCategoryItem selecteditem)
        {
            OnWheelClose?.Invoke(this, selectedCategory, selecteditem);
        }

        protected virtual void ItemTrigger(WheelCategory selectedCategory, WheelCategoryItem selecteditem)
        {
            OnItemTrigger?.Invoke(this, selectedCategory, selecteditem);
        }

        public void UnsubscribeAllEvents()
        {
            OnCategoryChange = null;
            OnItemChange = null;
            OnWheelOpen = null;
            OnWheelClose = null;
        }
    }

    public class WheelCategory
    {
        public string Name;
        public int CurrentItemIndex = 0;
        protected List<WheelCategoryItem> Items = new List<WheelCategoryItem>();
        public Vector2 position2D = Vector2.Zero;
        public Texture CategoryTexture;
        public Texture BackgroundTexture;
        public Texture HighlightTexture;
        public string Description { get; set; }

        /// <summary>
        /// Instantiates a new category for use in a selection wheel.
        /// </summary>
        /// <param name="name">Name of the category. If a matching .png image is found, the image will be displayed instead of any item image.</param>
        public WheelCategory(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Instantiates a new category for use in a selection wheel.
        /// </summary>
        /// <param name="name">Name of the category. If a matching .png image is found, the image will be displayed instead of any item image.</param>
        /// <param name="description">Category description. Only shown if there is no selected item description.</param>
        public WheelCategory(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Add item to this category.
        /// </summary>
        /// <param name="item">Item to add to this category</param>
        public void AddItem(WheelCategoryItem item)
        {
            Items.Add(item);
        }

        public void ClearAllItems()
        {
            Items.Clear();
        }

        public void RemoveItem(WheelCategoryItem item)
        {
            Items.Remove(item);
        }

        public int ItemCount()
        {
            return Items.Count;
        }

        public List<WheelCategoryItem> ItemList
        {
            get { return Items; }
        }

        public bool IsItemSelected(WheelCategoryItem item)
        {
            if (Items.Contains(item))
            {
                return Items.IndexOf(item) == CurrentItemIndex;
            }
            return false;
        }

        public WheelCategoryItem SelectedItem
        {
            get { return Items.ElementAt(CurrentItemIndex); }
        }

        public void GoToNextItem()
        {
            if (CurrentItemIndex < Items.Count - 1)
            {
                CurrentItemIndex++;
            }
            else
            {
                CurrentItemIndex = 0;
            }
        }

        public void GoToPreviousItem()
        {
            if (CurrentItemIndex > 0)
            {
                CurrentItemIndex--;
            }
            else
            {
                CurrentItemIndex = Items.Count - 1;
            }
        }
    }

    public class WheelCategoryItem
    {
        public string Name;
        public Texture ItemTexture;
        public string Description { get; set; }

        /// <summary>
        /// Instantiate a new item to be later added to a WheelCategory.
        /// </summary>
        /// <param name="name">Name of the item. If a matching .png image is found, the image will be displayed assuming no image for this item's category has been found.</param>
        public WheelCategoryItem(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Instantiate a new item to be later added to a WheelCategory.
        /// </summary>
        /// <param name="name">Name of the item. If a matching .png image is found, the image will be displayed assuming no image for this item's category has been found.</param>
        /// <param name="description">A description that will be displayed on the right side of the screen.</param>
        public WheelCategoryItem(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    public class Texture
    {
        public string Path { get; set; }
        public int Index { get; set; }
        public int DrawLevel { get; set; }

        public Texture(string path, int index)
        {
            Path = path;
            Index = index;
        }

        public void Draw(int level, int time, Point pos, Size size)
        {
            UI.DrawTexture(Path, Index, level, time, pos, size);
        }

        public void Draw(int level, int time, Point pos, Size size, float rotation, Color color)
        {
            UI.DrawTexture(Path, Index, level, time, pos, size, rotation, color);
        }

        public void Draw(int level, int time, Point pos, PointF center, Size size, float rotation, Color color)
        {
            UI.DrawTexture(Path, Index, level, time, pos, center, size, rotation, color);
        }

        public void Draw(int level, int time, Point pos, PointF center, Size size, float rotation, Color color, float aspectRatio)
        {
            UI.DrawTexture(Path, Index, level, time, pos, center, size, rotation, color, aspectRatio);
        }

        public void LoadTexture()
        {
            StopDraw();
        }
        
        public void StopDraw()
        {
            UI.DrawTexture(Path, Index, 1, 0, new Point(1280, 720), new Size(0, 0));
        }
    }

    public static class UIHelper
    {
        public enum TextJustification
        {
            Center = 0,
            Left,
            Right //requires SET_TEXT_WRAP
        }

        public static void DrawCustomText(string Message, float FontSize, Font FontType,
            int Red, int Green, int Blue, int Alpha, float XPos, float YPos,
            int dropShawdowPixelDistance, int dRed, int dGreen, int dBlue, int dAlpha,
            TextJustification justifyType = TextJustification.Left, bool ForceTextWrap = false, float startWrap = 0f, float endWrap = 1f,
            bool withRectangle = false, int R = 0, int G = 0, int B = 0, int A = 255,
            float rectWidthOffset = 0f, float rectHeightOffset = 0f, float rectYPosDivisor = 23.5f)
        {
            Function.Call(Hash._SET_TEXT_ENTRY, "jamyfafi"); //Required, don't change this! AKA BEGIN_TEXT_COMMAND_DISPLAY_TEXT
            Function.Call(Hash.SET_TEXT_SCALE, FontSize, FontSize); //1st param: 1.0f
            Function.Call(Hash.SET_TEXT_FONT, (int)FontType);
            Function.Call(Hash.SET_TEXT_COLOUR, Red, Green, Blue, Alpha);
            Function.Call(Hash.SET_TEXT_DROPSHADOW, dropShawdowPixelDistance, dRed, dGreen, dBlue, dAlpha);
            Function.Call(Hash.SET_TEXT_OUTLINE);
            Function.Call(Hash.SET_TEXT_JUSTIFICATION, (int)justifyType);
            if (justifyType == TextJustification.Right || ForceTextWrap)
            {
                Function.Call(Hash.SET_TEXT_WRAP, startWrap, endWrap);
            }

            //Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, Message);
            AddLongString(Message);

            Function.Call(Hash._DRAW_TEXT, XPos, YPos); //AKA END_TEXT_COMMAND_DISPLAY_TEXT

            if (withRectangle)
            {
                switch (FontType)
                {
                    case Font.ChaletLondon: rectYPosDivisor = 15f; break;
                    case Font.HouseScript: rectYPosDivisor = 23f; break;
                    case Font.Monospace: rectYPosDivisor = 25f; break;
                    case Font.ChaletComprimeCologne: rectYPosDivisor = 30f; break;
                    case Font.Pricedown: rectYPosDivisor = 35f; break;
                }

                float adjWidth = MeasureStringWidthNoConvert(Message, FontType, FontSize);
                float fontHeight = MeasureFontHeightNoConvert(FontSize, FontType);
                float rectangleWidth = (endWrap - startWrap) + rectWidthOffset;
                float baseYPos = YPos + (FontSize / rectYPosDivisor);
                //int numLines = (int)Math.Ceiling(adjWidth / ((endWrap - startWrap) * 0.98f));
                int numLines = GetStringLineCount(Message, FontSize, FontType, startWrap, endWrap, XPos, YPos);
                for (int i = 0; i < numLines; i++)
                {
                    float adjustedYPos = i == 0 ? baseYPos - rectHeightOffset / 2 
                        : (i == numLines - 1 ? baseYPos + rectHeightOffset / 2 
                        : baseYPos);

                    float adjustedRectangleHeight = i == 0 || i == numLines - 1 ? fontHeight + rectHeightOffset 
                        : fontHeight;

                    float adjustedXPos = justifyType == TextJustification.Left ? XPos + ((endWrap - startWrap) / 2) 
                        : (justifyType == TextJustification.Right ? endWrap - ((endWrap - startWrap) / 2) 
                        : XPos);

                    DrawRectangle(adjustedXPos, adjustedYPos + (i * fontHeight), rectangleWidth, adjustedRectangleHeight, R, G, B, A);
                }
            }
        }

        public static void DrawRectangle(float BgXpos, float BgYpos, float BgWidth, float BgHeight, int bgR, int bgG, int bgB, int bgA)
        {
            Function.Call(Hash.DRAW_RECT, BgXpos, BgYpos, BgWidth, BgHeight, bgR, bgG, bgB, bgA);
        }

        public static void AddLongString(string str)
        {
            const int strLen = 99;
            for (int i = 0; i < str.Length; i += strLen)
            {
                string substr = str.Substring(i, Math.Min(strLen, str.Length - i));
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, substr); //ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME
            }
        }

        public static float MeasureStringWidth(string str, Font font, float fontsize)
        {
            //int screenw = 2560;// Game.ScreenResolution.Width;
            //int screenh = 1440;// Game.ScreenResolution.Height;
            const float height = 1080f;
            float ratio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            float width = height * ratio;
            return MeasureStringWidthNoConvert(str, font, fontsize) * width;
        }

        private static float MeasureStringWidthNoConvert(string str, Font font, float fontsize)
        {
            Function.Call((Hash)0x54CE8AC98E120CAB, "jamyfafi"); //_BEGIN_TEXT_COMMAND_WIDTH
            AddLongString(str);
            Function.Call(Hash.SET_TEXT_FONT, (int)font);
            Function.Call(Hash.SET_TEXT_SCALE, fontsize, fontsize);
            return Function.Call<float>(Hash._0x85F061DA64ED2F67, true); //_END_TEXT_COMMAND_GET_WIDTH //Function.Call<float>((Hash)0x85F061DA64ED2F67, (int)font) * fontsize; //_END_TEXT_COMMAND_GET_WIDTH
        }

        public static float MeasureFontHeight(float fontSize, Font font)
        {
            return Function.Call<float>(Hash._0xDB88A37483346780, fontSize, (int)font) * Game.ScreenResolution.Height; //1080f
        }

        public static float MeasureFontHeightNoConvert(float fontSize, Font font)
        {
            return Function.Call<float>(Hash._0xDB88A37483346780, fontSize, (int)font);
        }

        public static int GetStringLineCount(string text, float FontSize, Font FontType, float startWrap, float endWrap, float x, float y)
        {
            Function.Call((Hash)0x521FB041D93DD0E4, "jamyfafi"); //_BEGIN_TEXT_COMMAND_LINE_COUNT
            Function.Call(Hash.SET_TEXT_SCALE, FontSize, FontSize); //1st param: 1.0f
            Function.Call(Hash.SET_TEXT_FONT, (int)FontType);
            Function.Call(Hash.SET_TEXT_WRAP, startWrap, endWrap);
            AddLongString(text);
            return Function.Call<int>((Hash)0x9040DFB09BE75706, x, y); //_END_TEXT_COMMAND_GET_LINE_COUNT
        }

        public static float XPixelToPercentage(int pixel)
        {
            const float height = 1080f;
            float ratio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            float width = height * ratio;

            return pixel / width;
        }

        public static float YPixelToPercentage(int pixel)
        {
            const float height = 1080f;
            float ratio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            float width = height * ratio;

            return pixel / height;
        }

        public static float XPercentageToPixel(float percent)
        {
            const float height = 1080f;
            float ratio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            float width = height * ratio;

            return percent * width;
        }

        public static float YPercentageToPixel(float percent)
        {
            const float height = 1080f;
            float ratio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            float width = height * ratio;

            return percent * height;
        }

        public static string MakeValidFileName(string original, char replacementChar = '_')
        {
            var invalidChars = new HashSet<char>(System.IO.Path.GetInvalidFileNameChars());
            return new string(original.Select(c => invalidChars.Contains(c) ? replacementChar : c).ToArray());
        }
        
        public static float AspectRatio { get; private set; } = Function.Call<float>(Hash._GET_SCREEN_ASPECT_RATIO, true);

        public static float UpdateAspectRatio()
        {
            AspectRatio = Function.Call<float>(Hash._GET_SCREEN_ASPECT_RATIO, true);
            return AspectRatio;
        }

        public static PointF PointFromCenter(float x, float y, float normalizedAngle)
        {
            // Credits to MaxShadow for this method
            float angle2 = (normalizedAngle * (float)Math.PI * 2) - ((float)Math.PI / 2);
            float x2 = (UI.WIDTH / 2) + (float)Math.Cos(angle2) * x;
            float y2 = (UI.HEIGHT / 2) + (float)Math.Sin(angle2) * y * (AspectRatio / (16f / 9f));
            return new PointF(x2, y2);
        }
    }
}