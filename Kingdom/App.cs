using Bridge;
using Bridge.Html5;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

// Než se zděsíte, co za obludnost jsem to spáchal, podívejte se do ostatních souborů :-) Mělo by to tam vypadat trochu lépe

namespace Kingdom
{
    public class App
    {
        public static bool isSaved = true;

        public static bool debug = false;
        public static bool useFog = false;
        public static int tiles = 64;
        public static int? textureMapTextureLength = 128;
        private static bool runAnim = true;
        private static double animOneFrame = 0.1;
        private static int visibleTiles = 20;
        private static bool gameEnded = false;
        private static bool isMenuVisible = false;
        private static double audioVolume;

        // I'm using auto-generated texture maps (adding multiple images to one big) (performance improvements - in chrome from 5.5x to 16.9x better performance), here's where I set order of textures
        public enum Textures
        {
            border,
            border_blue,
            border_green,
            border_red,
            border_yellow,
            city_blue,
            city_green,
            city_neutral,
            city_red,
            city_yellow,
            cloud,
            city_recruitTent_blue,
            city_recruitTent_green,
            city_recruitTent_red,
            city_recruitTent_yellow,
            resource_bush,
            resource_deer,
            resource_farm,
            resource_farm_done,
            resource_fish,
            terrain_land,
            terrain_mountain,
            terrain_sea,
            unavalibeTile,
            unit_archer,
            unit_basic_blue,
            unit_catapult,
            unit_explorer,
            unit_basic_green,
            unit_heavyInfantry,
            unit_musketeer,
            unit_basic_red,
            unit_warrior,
            unit_basic_yellow,
            unit_berserk
        }
        public static HTMLImageElement txMap;

        private static HTMLHeadingElement info_head;
        private static HTMLDivElement info_text;

        private static HTMLCanvasElement canvas;
        private static HTMLCanvasElement infoCanvas;
        private static CanvasRenderingContext2D ctx;
        private static CanvasRenderingContext2D infoCtx;
        private static int width;
        private static int height;

        private static double _offsetX = 0;
        private static double _offsetY = 0;
        private static double OffsetX
        {
            get { return _offsetX; }
            set
            {
                if (value != _offsetX)
                {
                    if (debug)
                        Script.Call("console.log", $"Changing offsetX from {_offsetX} to {value}. Max: {tiles - visibleTiles}");
                    if (value > 0)
                        _offsetX = 0;
                    else if (-value >= tiles - visibleTiles)
                        _offsetX = visibleTiles - tiles;
                    else
                        _offsetX = value;
                }
            }
        }
        private static double OffsetY
        {
            get { return _offsetY; }
            set
            {
                if (value != _offsetY)
                {
                    if (debug)
                        Script.Call("console.log", $"Changing offsetY from {_offsetY} to {value}. Max: {tiles - visibleTiles}");
                    if (value > 0)
                        _offsetY = 0;
                    else if (-value >= tiles - visibleTiles)
                        _offsetY = visibleTiles - tiles;
                    else
                        _offsetY = value;
                }
            }
        }
        private static List<char> pressedOffsetKeys = new List<char>();
        private static double offsetAnimSpeed = 0.2;
        private static bool isOffsetAnimRunning = false;

        private static int _killedInDefense;
        public static int KilledInDefense
        {
            get
            {
                return _killedInDefense;
            }
            set
            {
                _killedInDefense = value;
                if (KilledInDefense >= 10)
                {
                    Achievement.achievements[(int)Achievement.eAchievs.YouShallNotPass].isSpecial = false;
                    Achievement.Check();
                }
            }
        }

        private static void OffsetAnimFrame()
        {
            isOffsetAnimRunning = true;

            if (pressedOffsetKeys.Contains('w'))
                OffsetY += offsetAnimSpeed;
            if (pressedOffsetKeys.Contains('s'))
                OffsetY -= offsetAnimSpeed;
            if (pressedOffsetKeys.Contains('a'))
                OffsetX += offsetAnimSpeed;
            if (pressedOffsetKeys.Contains('d'))
                OffsetX -= offsetAnimSpeed;

            if (pressedOffsetKeys.Count > 0)
            {
                Draw();
                Window.RequestAnimationFrame(OffsetAnimFrame);
            }
            else
            {
                // Offset has to be x € Z, when not, Click() (I want to know on what tile you clicked) is one big bug. It calcs bad tiles.

                bool redraw = false;
                if (OffsetX != Math.Round(OffsetX))
                {
                    redraw = true;
                    OffsetX = Math.Round(OffsetX);
                }
                if (OffsetY != Math.Round(OffsetY))
                {
                    redraw = true;
                    OffsetY = Math.Round(OffsetY);
                }
                if (redraw)
                    Draw();

                isOffsetAnimRunning = false;
            }
        }

        private static bool isAnimRunning = false;

        public static void Main()
        {
            string fileName = Window.Location.PathName.Split('/').Pop().ToString();
            Achievement.Load();
            if (fileName == "game.html")
            {
                Document.Body.OnLoad = (ev) => { Initialize(); CalcInfoWidth(); };
                Document.Body.OnResize = (ev) => { CalcInfoWidth(); };
            }
            else if (fileName == "Achievement.html")
            {
                Document.GetElementById("achievList").InnerHTML = Achievement.getAchievHTML();
                Document.Body.OnKeyDown = (ev) =>
                {
                    if (ev.Key == "Escape")
                        Script.Eval("window.location = 'index.html'");
                };
            }
        }

        private static void Initialize()
        {
            txMap = Document.GetElementById("textureMap") as HTMLImageElement;
            new textureMap(txMap);

            info_head = Document.GetElementById("info_head") as HTMLHeadingElement;
            info_text = Document.GetElementById("info_text") as HTMLDivElement;

            announcement = Document.GetElementById("announcement") as HTMLDivElement;

            string save = (Window.LocalStorage["game_continue"] ?? "").ToString();
            debug = Window.LocalStorage["kingdom-debug_mode"] != null && Window.LocalStorage["kingdom-debug_mode"].ToString() == "true";
            useFog = !(Window.LocalStorage["kingdom-begin_fog"] != null && Window.LocalStorage["kingdom-begin_fog"].ToString() == "false");
            try
            {
                animOneFrame = double.Parse(Window.LocalStorage["kingdom-move_anim_speed"].ToString());
                if (animOneFrame < 0)
                    animOneFrame = 0;
                if (animOneFrame > 1)
                    animOneFrame = 1;

                if (animOneFrame == 0)
                    runAnim = false;
            }
            catch { }
            try
            {
                offsetAnimSpeed = double.Parse(Window.LocalStorage["kingdom-offset_anim_speed"].ToString());
                if (offsetAnimSpeed < 0)
                    offsetAnimSpeed = 0;
                if (offsetAnimSpeed > 1)
                    offsetAnimSpeed = 1;
            }
            catch { }
            try
            {
                tiles = int.Parse(Window.LocalStorage["kingdom-number_of_tiles"].ToString());
                if (tiles < 40)
                    tiles = 40;
                if (tiles > 256)
                    tiles = 256;
            }
            catch { }
            try
            {
                visibleTiles = int.Parse(Window.LocalStorage["kingdom-visible_tiles"].ToString());
                if (visibleTiles < 10)
                    visibleTiles = 10;
                if (visibleTiles > 50)
                    visibleTiles = 50;
            }
            catch { }
            int numberOfOpponents = 1;
            try
            {
                numberOfOpponents = int.Parse(Window.LocalStorage["kingdom-number_opponents"].ToString());
                if (numberOfOpponents < 1)
                    numberOfOpponents = 1;
                if (numberOfOpponents > 3)
                    numberOfOpponents = 3;
            }
            catch { }
            bool playAi = Window.LocalStorage["kingdom-play_against_ai"] != null && Window.LocalStorage["kingdom-play_against_ai"].ToString() == "true";
            if (visibleTiles > tiles)
                visibleTiles = tiles;

            new Game(tiles, save, numberOfOpponents + 1, playAi);

            canvas = Document.GetElementById("canvas") as HTMLCanvasElement;
            infoCanvas = Document.GetElementById("infoCanvas") as HTMLCanvasElement;
            ctx = canvas.GetContext("2d").As<CanvasRenderingContext2D>();
            ctx.ImageSmoothingEnabled = false;
            infoCtx = infoCanvas.GetContext("2d").As<CanvasRenderingContext2D>();
            ctx.Font = "40px sans-serif";
            width = canvas.Width / visibleTiles;
            height = canvas.Height / visibleTiles;

            OffsetX = -Game.game.Map.OfType<MapObject>().Where(x => x is City).Select(x => x as City).Where(x => x.owner == Game.game.players[Game.game.playerState]).First().x + visibleTiles / 2;
            OffsetY = -Game.game.Map.OfType<MapObject>().Where(x => x is City).Select(x => x as City).Where(x => x.owner == Game.game.players[Game.game.playerState]).First().y + visibleTiles / 2;

            Draw();

            canvas.OnClick = (ev) => { ClickTo(ev.LayerX, ev.LayerY, ev.CtrlKey, ev.AltKey); };

            Document.OnKeyDown = (ev) =>
            {
                if (isMenuVisible)
                {
                    if (ev.Key == "Escape")
                    {
                        isMenuVisible = false;
                        Document.GetElementsByClassName("gameEscMenu")[0].Remove();
                    }
                    return;
                }

                if (ev.Key == "w" && !pressedOffsetKeys.Contains('w')) pressedOffsetKeys.Add('w');
                if (ev.Key == "s" && !pressedOffsetKeys.Contains('s')) pressedOffsetKeys.Add('s');
                if (ev.Key == "a" && !pressedOffsetKeys.Contains('a')) pressedOffsetKeys.Add('a');
                if (ev.Key == "d" && !pressedOffsetKeys.Contains('d')) pressedOffsetKeys.Add('d');
                if (ev.Key == "Escape")
                {
                    isMenuVisible = true;
                    HTMLDivElement menu = new HTMLDivElement();
                    menu.ClassName = "gameEscMenu";
                    HTMLDivElement innerMenu = new HTMLDivElement();
                    innerMenu.AppendChild(new HTMLHeadingElement(HeadingType.H2) { InnerHTML = "Menu" });
                    innerMenu.AppendChild(new HTMLButtonElement { InnerHTML = "Back to Game", OnClick = (e) => { menu.Remove(); isMenuVisible = false; } });
                    innerMenu.AppendChild(new HTMLButtonElement
                    {
                        InnerHTML = "Save and Exit",
                        OnClick = (e) =>
                        {
                            Document.GetElementById("loadingScreen").SetAttribute("style", "display: block;");
                            Document.GetElementById("details").InnerHTML = "This will take a while. I have to save " + (Game.game.Map.OfType<MapObject>().Count() + Game.game.players.Count + 1) + " objects";
                            menu.Remove();

                            Window.SetTimeout(() =>
                            {
                                string text = SaveManager.Save();
                                Window.LocalStorage["game_continue"] = text;
                                isSaved = true;
                                Document.GetElementById("loadingScreen").SetAttribute("style", "display: none;");
                                Script.Eval("window.location = 'index.html';");
                            }, 500);
                        }
                    });
                    innerMenu.AppendChild(new HTMLButtonElement { InnerHTML = "Exit to Main Menu", OnClick = (e) => { Script.Eval("window.location = 'index.html';"); } });
                    innerMenu.AppendChild(new HTMLHRElement());
                    innerMenu.AppendChild(new HTMLHeadingElement(HeadingType.H2) { InnerHTML = "Quick Settings" });
                    innerMenu.AppendChild(new HTMLParagraphElement { InnerHTML = "Music Volume: <input style='margin:1%; width: 20%;' type='range' min=0 max=1 step=0.01 value=" + audioVolume + " onchange='document.getElementsByTagName(\"audio\")[0].volume = this.value;' />" });
                    HTMLParagraphElement p = new HTMLParagraphElement { InnerHTML = "Visible Tiles: " };
                    HTMLInputElement input = new HTMLInputElement();
                    input.SetAttribute("style", "margin: 1%; width: 20%;");
                    input.Type = InputType.Range;
                    input.Min = "10";
                    input.Max = tiles.ToString();
                    input.Step = "1";
                    input.Value = visibleTiles.ToString();
                    input.OnChange = (e) =>
                    {
                        try
                        {
                            int value = int.Parse(input.Value);
                            if (value < 10 || value > 50 || value > tiles)
                                return;
                            visibleTiles = value;
                            width = canvas.Width / visibleTiles;
                            height = canvas.Height / visibleTiles;
                            OffsetX = -Game.game.Map.OfType<MapObject>().Where(x => x is City).Select(x => x as City).Where(x => x.owner == Game.game.players[Game.game.playerState]).First().x + visibleTiles / 2;
                            OffsetY = -Game.game.Map.OfType<MapObject>().Where(x => x is City).Select(x => x as City).Where(x => x.owner == Game.game.players[Game.game.playerState]).First().y + visibleTiles / 2;
                            Draw();
                        }
                        catch { }
                    };
                    p.AppendChild(input);
                    innerMenu.AppendChild(p);
                    menu.AppendChild(innerMenu);
                    Document.Body.AppendChild(menu);
                }

                if (!isOffsetAnimRunning && pressedOffsetKeys.Count > 0 && offsetAnimSpeed != 0)
                    OffsetAnimFrame();
            };

            Document.OnKeyUp = (ev) =>
            {
                if (gameEnded || isMenuVisible)
                    return;

                if (ev.Key == "Enter") Document.GetElementById("nextTurn").Click();

                if (offsetAnimSpeed == 0)
                {
                    if (ev.Key == "d") { OffsetX -= 3; Draw(); }
                    if (ev.Key == "a") { OffsetX += 3; Draw(); }
                    if (ev.Key == "w") { OffsetY += 3; Draw(); }
                    if (ev.Key == "s") { OffsetY -= 3; Draw(); }
                }
                else
                {
                    if (ev.Key == "w" && pressedOffsetKeys.Contains('w')) pressedOffsetKeys.Remove('w');
                    if (ev.Key == "s" && pressedOffsetKeys.Contains('s')) pressedOffsetKeys.Remove('s');
                    if (ev.Key == "a" && pressedOffsetKeys.Contains('a')) pressedOffsetKeys.Remove('a');
                    if (ev.Key == "d" && pressedOffsetKeys.Contains('d')) pressedOffsetKeys.Remove('d');
                }
            };

            Document.GetElementById("research").OnClick = (ev) => { if (isMenuVisible) return; ResearchSomething(); Document.GetElementById("research").Blur(); };


            // TODO: If mouse is near edge of canvas, offset a little
            Document.GetElementById("nextTurn").OnClick = (ev) =>
            {
                if (isAnimRunning || isOffsetAnimRunning || announceAnimIsRunning || isMenuVisible)
                    return;

                Game.game.NextTurn(Game.game.players[Game.game.playerState]);

                Player p = Game.game.players[Game.game.playerState];

                // If player has alredy lost, skip
                if (p.end)
                    Document.GetElementById("nextTurn").Click();
                // If player has no cities, he lost
                if (Game.game.Map.OfType<MapObject>().Where(x => x is City).Where(x => (x as City).owner == p).Count() == 0)
                {
                    p.end = true;
                    Document.GetElementById("nextTurn").Click();
                }
                // If everyone except one player lost OR there is no human player left
                if (Game.game.players.Where(x => x.end).Count() == Game.game.players.Count - 1 || Game.game.players.Where(x => x.currAI == null && !x.end).Count() == 0)
                {
                    Announce("<h1>Game Over</h1><p>The game has ended. Return to <a href='index.html'>Main Menu</a> and try another game!", true);
                    gameEnded = true;
                    isSaved = true;
                    Achievement.finishedGames++;
                    Achievement.Check();
                    Window.LocalStorage["game_continue"] = "";
                    return;
                }

                CloseRecruitBuildResearch();

                Document.GetElementById("nextTurn").Blur();



                if (p.currAI == null)
                {
                    Script.Call("console.log", "Player is marked as AI, skipping player name change & map centering");
                    DisplayPlayerInfo();
                    try
                    {
                        OffsetX = -Game.game.Map.OfType<MapObject>().Where(x => x is Unit).Select(x => x as Unit).Where(x => x.owner == p).First().x + visibleTiles / 2;
                        OffsetY = -Game.game.Map.OfType<MapObject>().Where(x => x is Unit).Select(x => x as Unit).Where(x => x.owner == p).First().y + visibleTiles / 2;
                    }
                    catch
                    {
                        // Player has no units
                    }
                }
                Draw();
                info_text.InnerHTML = "";
                info_head.InnerHTML = "";
                selectedUnit = null;
                infoCtx.ClearRect(0, 0, 64, 64);
                if (p.currAI != null && false)
                {
                    p.currAI.Play();
                    Window.SetTimeout(Document.GetElementById("nextTurn").Click);
                    return;
                }
                else
                {
                    //Announce($"<h1>Next Turn</h1><p>It's turn of <span style='color: {(p.color == 0 ? "blue" : (p.color == 1 ? "red" : (p.color == 2 ? "green" : "orange")))}>Player {p.color}</span>. Click outside this button to continue.", true);
                }

                announceTexts.Clear();
                isSaved = false;
            };

            // Initialize 'simplified version' of this game
            new Pong();

            DisplayPlayerInfo();

            audioVolume = 0.1;
            try
            {
                double d = double.Parse(Window.LocalStorage["kingdom-music_volume"].ToString());
                if (d >= 0 && d <= 1)
                    audioVolume = d;
            }
            catch { }

            HTMLAudioElement a = (Document.GetElementsByTagName("audio")[0] as HTMLAudioElement);
            a.Volume = audioVolume;
            a.AutoPlay = true;
            a.Play();

            if (debug)
                Script.Call("console.log", "Initialized");

            Document.GetElementById("save").OnClick = (ev) =>
            {
                if (isMenuVisible)
                    return;

                Document.GetElementById("save").Blur();
                Document.GetElementById("loadingScreen").SetAttribute("style", "display: block;");
                Document.GetElementById("details").InnerHTML = "This will take a while. I have to save " + (Game.game.Map.OfType<MapObject>().Count() + Game.game.players.Count + 1) + " objects";

                Window.SetTimeout(() =>
                {
                    string text = SaveManager.Save();
                    Window.LocalStorage["game_continue"] = text;
                    isSaved = true;
                    Document.GetElementById("loadingScreen").SetAttribute("style", "display: none;");
                }, 500);
            };
        }

        private static void CalcInfoWidth()
        {
            Document.GetElementById("aside").Style.Width = (Document.Body.OffsetWidth - canvas.OffsetWidth).ToString() + "px";
        }

        private static void Draw(bool displayTextInfo = true, int targetX = -1, int targetY = -1)
        {
            Stopwatch s = new Stopwatch();
            if (debug)
            {
                //Script.Call("console.log", "Drawing...");
                s.Start();
            }

            if (targetX == -1 || targetY == -1)
            {
                for (int x = 0; x < visibleTiles + -OffsetX + (OffsetX == 0 ? 0 : (OffsetX < 0 ? 1 : -1)); x++)
                {
                    if (x >= tiles)
                        break;
                    for (int y = 0; y < visibleTiles + -OffsetY + (OffsetY == 0 ? 0 : (OffsetY < 0 ? 1 : -1)); y++)
                    {
                        if (y >= tiles)
                            break;

                        if (useFog && (Game.game.Map[x, y, 0] as Terrain).isFogForPlayers[Game.game.playerState])
                        {
                            Point po = textureMap.txMap.getSubImageCoords(Textures.cloud);
                            ctx.DrawImage(txMap, po.x, po.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);
                            continue;
                        }

                        Point p = textureMap.txMap.getSubImageCoords(Game.game.Map[x, y, 0].texture);
                        ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);
                        if (Game.game.Map[x, y, 1] != null)
                        {
                            p = textureMap.txMap.getSubImageCoords(Game.game.Map[x, y, 1].texture);
                            ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);

                            if (Game.game.Map[x, y, 1] is City && displayTextInfo)
                                ctx.FillText($"{(Game.game.Map[x, y, 1] as City).Currpop} / {(Game.game.Map[x, y, 1] as City).Maxpop}", (int)((x + OffsetX) * width), (int)((y + OffsetY) * height));
                        }
                        if (Game.game.Map[x, y, 2] != null)
                        {
                            p = textureMap.txMap.getSubImageCoords(Game.game.Map[x, y, 2].texture);
                            ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);

                            Textures frame = Textures.unavalibeTile;
                            Unit u = Game.game.Map[x, y, 2] as Unit;
                            switch (u.owner.color)
                            {
                                case 0:
                                    frame = Textures.border_blue;
                                    break;
                                case 1:
                                    frame = Textures.border_red;
                                    break;
                                case 2:
                                    frame = Textures.border_green;
                                    break;
                                case 3:
                                    frame = Textures.border_yellow;
                                    break;
                            }


                            p = textureMap.txMap.getSubImageCoords(u.weaponTexture);

                            ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);
                            if ((Game.game.Map[x, y, 2] as Unit).turns > 0 || ((Game.game.Map[x, y, 2] as Unit).canAttack && false /* TODO: Check if there is any enemy around him */))
                            {
                                p = textureMap.txMap.getSubImageCoords(frame);
                                ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);
                            }
                            if (displayTextInfo)
                            //ctx.FillText(u.hp.ToString(), (int)((x + offsetX) * width), (int)((y + offsetY) * height));
                            {
                                var fillStyle = ctx.FillStyle;
                                ctx.FillStyle = InterpolateRedGreenColor(u.hp, u.maxhp);
                                ctx.FillRect((int)((x + OffsetX) * width + width / 5), (int)((y + OffsetY) * height + height / 5), 15, 15);
                                ctx.FillStyle = fillStyle;
                            }
                        }
                    }
                }
            }
            else
            {
                int x = targetX;
                int y = targetY;

                Point p = textureMap.txMap.getSubImageCoords(Game.game.Map[x, y, 0].texture);
                ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);
                if (Game.game.Map[x, y, 1] != null)
                {
                    p = textureMap.txMap.getSubImageCoords(Game.game.Map[x, y, 1].texture);
                    ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);
                }
                if (Game.game.Map[x, y, 2] != null)
                {
                    p = textureMap.txMap.getSubImageCoords(Game.game.Map[x, y, 2].texture);
                    ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);

                    Textures frame = Textures.unavalibeTile;
                    Unit u = Game.game.Map[x, y, 2] as Unit;
                    switch (u.owner.color)
                    {
                        case 0:
                            frame = Textures.border_blue;
                            break;
                        case 1:
                            frame = Textures.border_red;
                            break;
                        case 2:
                            frame = Textures.border_green;
                            break;
                        case 3:
                            frame = Textures.border_yellow;
                            break;
                    }


                    p = textureMap.txMap.getSubImageCoords(u.weaponTexture);

                    ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);
                    if ((Game.game.Map[x, y, 2] as Unit).turns > 0 || ((Game.game.Map[x, y, 2] as Unit).canAttack && false /* TODO: Check if there is any enemy around him */))
                    {
                        p = textureMap.txMap.getSubImageCoords(frame);
                        ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);
                    }
                    if (displayTextInfo)
                    //ctx.FillText(u.hp.ToString(), (int)((x + offsetX) * width), (int)((y + offsetY) * height));
                    {
                        var fillStyle = ctx.FillStyle;
                        ctx.FillStyle = InterpolateRedGreenColor(u.hp, u.maxhp);
                        ctx.FillRect((int)((x + OffsetX) * width + width / 5), (int)((y + OffsetY) * height + height / 5), 15, 15);
                        ctx.FillStyle = fillStyle;
                    }
                }
            }

            if (debug)
            {
                s.Stop();
                Script.Call("console.log", $"Draw finished in " + s.ElapsedMilliseconds + " ms");
            }
        }

        private static void DrawPossibleUnitMoves(Unit unit)
        {
            if (debug)
                Script.Call("console.log", "Drawing possible unit moves");

            for (int x = 0; x < visibleTiles + -OffsetX + (OffsetX == 0 ? 0 : (OffsetX < 0 ? 1 : -1)); x++)
            {
                if (x >= tiles)
                    break;
                for (int y = 0; y < visibleTiles + -OffsetY + (OffsetY == 0 ? 0 : (OffsetY < 0 ? 1 : -1)); y++)
                {
                    if (y >= tiles)
                        break;
                    if ((useFog && (Game.game.Map[x, y, 0] as Terrain).isFogForPlayers[Game.game.playerState]) || (Math.Max(Math.Abs(unit.x - x), Math.Abs(unit.y - y)) > unit.turns && (!unit.canAttackToSomebody(x, y) || Math.Max(Math.Abs(unit.x - x), Math.Abs(unit.y - y)) > unit.turns + 1)) || !unit.canMove(x, y, unit.owner))
                    {
                        Point p = textureMap.txMap.getSubImageCoords(Textures.unavalibeTile);
                        ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);
                    }
                }
            }
        }

        public static void Anim(int xFrom, int yFrom, int xTo, int yTo, Unit unit, Action endOfAnimTrigger)
        {
            if (!runAnim)
            {
                endOfAnimTrigger.Call();
                return;
            }

            if (debug)
                Script.Call("console.log", $"Anim started. xFrom: {xFrom}, yFrom: {yFrom}, xTo: {xTo}, yTo: {yTo}");

            isAnimRunning = true;

            Textures[] texture = { unit.texture, unit.weaponTexture };

            Draw(false);


            Point p = textureMap.txMap.getSubImageCoords(Game.game.Map[xTo, yTo, 0].texture);
            ctx.ClearRect((int)((xTo + OffsetX) * width), (int)((yTo + OffsetY) * height), width, height);
            ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (xTo + OffsetX) * width, (yTo + OffsetY) * height, width, height);
            if (Game.game.Map[xTo, yTo, 1] != null)
            {
                p = textureMap.txMap.getSubImageCoords(Game.game.Map[xTo, yTo, 1].texture);
                ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (xTo + OffsetX) * width, (yTo + OffsetY) * height, width, height);
            }

            Action frame = () => { AnimFrame(texture, xFrom, yFrom, xTo, yTo, xFrom, yFrom, unit, endOfAnimTrigger); };

            Window.RequestAnimationFrame(frame);
        }

        private static void AnimFrame(Textures[] texture, int xFrom, int yFrom, int xTo, int yTo, double x, double y, Unit unit, Action endOfAnimTrigger)
        {
            if (debug)
                Script.Call("console.log", $"AnimFrame called with x: {x} and y: {y}");


            int nX = (int)Math.Round(x);
            int nY = (int)Math.Round(y);

            Point p;

            for (int i = nX - 1; i <= nX + 1; i++)
            {
                if (i < 0 || i >= tiles)
                    continue;
                for (int j = nY - 1; j <= nY + 1; j++)
                {
                    if (j < 0 || j >= tiles)
                        continue;

                    p = textureMap.txMap.getSubImageCoords(Game.game.Map[i, j, 0].texture);
                    ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (i + OffsetX) * width, (j + OffsetY) * height, width, height);
                    if (Game.game.Map[i, j, 1] != null)
                    {
                        p = textureMap.txMap.getSubImageCoords(Game.game.Map[i, j, 1].texture);
                        ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (i + OffsetX) * width, (j + OffsetY) * height, width, height);
                    }
                    if (Game.game.Map[i, j, 2] != null && Game.game.Map[i, j, 2] != unit)
                    {
                        p = textureMap.txMap.getSubImageCoords(Game.game.Map[i, j, 2].texture);
                        ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (i + OffsetX) * width, (j + OffsetY) * height, width, height);
                        p = textureMap.txMap.getSubImageCoords((Game.game.Map[i, j, 2] as Unit).weaponTexture);
                        ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (i + OffsetX) * width, (j + OffsetY) * height, width, height);
                    }
                }
            }
            if (x < xTo)
                x += animOneFrame;
            else if (x > xTo)
                x -= animOneFrame;

            if (y < yTo)
                y += animOneFrame;
            else if (y > yTo)
                y -= animOneFrame;

            if (debug)
                Script.Call("console.log", $"AnimFrame extended to x: {x} and y: {y}");

            foreach (Textures t in texture /* this is better then [texture t in textures] */)
            {
                p = textureMap.txMap.getSubImageCoords(t);
                ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (x + OffsetX) * width, (y + OffsetY) * height, width, height);
            }

            bool xIsThere = false;
            bool yIsThere = false;

            if (Math.Abs(x - xTo) <= animOneFrame)
            {
                xIsThere = true;
                x = xTo;
            }
            if (Math.Abs(y - yTo) <= animOneFrame)
            {
                yIsThere = true;
                y = yTo;
            }

            if (!(xIsThere && yIsThere))
            {
                Action frame = () => { AnimFrame(texture, xFrom, yFrom, xTo, yTo, x, y, unit, endOfAnimTrigger); };

                Window.RequestAnimationFrame(frame);
            }
            else
            {
                if (debug)
                    Script.Call("console.log", "Anim ended");

                isAnimRunning = false;
                endOfAnimTrigger.Call();
            }
        }

        private static void ClickTo(int x, int y, bool ctrlWasPressed, bool altWasPressed)
        {
            if (isAnimRunning || isOffsetAnimRunning || isMenuVisible)
                return;

            //Draw();

            double xTileD = (Math.Floor((x) / (canvas.OffsetWidth / (double)visibleTiles))) + -OffsetX;
            double yTileD = (Math.Floor((y) / (canvas.OffsetHeight / (double)visibleTiles))) + -OffsetY;

            //xTile += (int)Math.Round(-offsetX);
            //yTile += (int)Math.Round(-offsetY);

            int xTile = (int)Math.Floor(xTileD);
            int yTile = (int)Math.Floor(yTileD);

            if (debug)
                Script.Call("console.log", $"Clicked to {x}, {y}. Tiles: {xTile}, {yTile}. CTRL: {ctrlWasPressed}. ALT: {altWasPressed}");

            MapObject toDisplay = Game.game.Map[xTile, yTile, 0];
            // If there is ONLY resource, or there is both UNIT and RESOURCE and user has right-clicked
            if ((Game.game.Map[xTile, yTile, 1] != null && Game.game.Map[xTile, yTile, 2] == null) ||
                (Game.game.Map[xTile, yTile, 1] != null && Game.game.Map[xTile, yTile, 2] != null && ctrlWasPressed))
                toDisplay = Game.game.Map[xTile, yTile, 1];
            else if (Game.game.Map[xTile, yTile, 2] != null)
                toDisplay = Game.game.Map[xTile, yTile, 2];

            DisplayInfoAbout(toDisplay, Game.game.players[Game.game.playerState], x, y, ctrlWasPressed, altWasPressed);
        }

        private static Unit selectedUnit;
        private static void DisplayInfoAbout(MapObject obj, Player currentPlayer, int refreshX, int refreshY, bool refreshCtrl, bool altWasPressed)
        {
            if (debug)
                Script.Call("console.log", $"Displaying info about {obj}");

            if (!altWasPressed && !refreshCtrl && selectedUnit != null && selectedUnit != obj)
            {
                int bX = selectedUnit.x;
                int bY = selectedUnit.y;
                if (selectedUnit.Move(obj.x, obj.y, currentPlayer))
                {
                    //Script.Call("console.log", selectedUnit.canAttackToSomebody());
                    //Script.Call("console.log", $"Anim would be called with {bX}, {bY}, {obj.x}, {obj.y}, selectedUnit");
                    Anim(bX, bY, obj.x, obj.y, selectedUnit, () => { for (int i = obj.x - 1; i <= obj.x + 1; i++) { if (i < 0 || i >= tiles) continue; for (int j = obj.y - 1; j <= obj.y + 1; j++) { if (j < 0 || j >= tiles) continue; Draw(true, i, j); } } if (selectedUnit.turns > 0 || selectedUnit.canAttackToSomebody()) { DrawPossibleUnitMoves(selectedUnit); DisplayInfoAbout(obj, currentPlayer, refreshX, refreshY, refreshCtrl, altWasPressed); } else { selectedUnit = null; Draw(); } });

                    // TODO: Make this work
                    //Path path = new Path(bX, bY, obj.x, obj.y, selectedUnit);
                    //Script.Call("console.log", path.pth);
                    //if (path.pth.Count > 1 /* TOOD: 1 or 0? */)
                    {/*
                        int lastX = bX;
                        int lastY = bY;
                        for (int i = 0; i < path.pth.Count - 1; i++)
                        {
                            Anim(lastX, lastY, path.pth[i].x, path.pth[i].y, selectedUnit, () => { });
                            while (isAnimRunning) { }
                            lastX = path.pth[i].x;
                            lastY = path.pth[i].y;
                        }
                        Anim(lastX, lastY, path.pth.Last().x, path.pth.Last().y, selectedUnit, () => { for (int i = obj.x - 1; i <= obj.x + 1; i++) { if (i < 0 || i >= tiles) continue; for (int j = obj.y - 1; j <= obj.y + 1; j++) { if (j < 0 || j >= tiles) continue; Draw(true, i, j); } } if (selectedUnit.turns > 0 || selectedUnit.canAttackToSomebody()) { DrawPossibleUnitMoves(selectedUnit); DisplayInfoAbout(obj, currentPlayer, refreshX, refreshY, refreshCtrl, altWasPressed); } else { selectedUnit = null; Draw(); } });
                  */

                        // TODO: Enable when TODO in l. 666 (?) (few lines up here) done
                        //AnimSteps(bX, bY, path, 1, obj, currentPlayer, refreshX, refreshY, refreshCtrl, altWasPressed);
                    }
                }
                else
                {
                    if (debug)
                        Script.Call("console.log", "Selected unit cannot move here");
                    //Draw();
                    ClickTo(refreshX, refreshY, refreshCtrl, true);
                }
                return;
            }
            else
                selectedUnit = null;


            Draw();

            Point p = textureMap.txMap.getSubImageCoords(Textures.border);
            ctx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, (obj.x + OffsetX) * width, (obj.y + OffsetY) * height, width, height);

            info_head.InnerHTML = "";
            info_text.InnerHTML = "";

            p = textureMap.txMap.getSubImageCoords(obj.texture);
            infoCtx.ClearRect(0, 0, 64, 64);
            infoCtx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, 0, 0, (int?)64, (int?)64);

            if (useFog && (Game.game.Map[obj.x, obj.y, 0] as Terrain).isFogForPlayers[Game.game.playerState])
            {
                p = textureMap.txMap.getSubImageCoords(Textures.cloud);
                infoCtx.ClearRect(0, 0, 64, 64);
                infoCtx.DrawImage(txMap, p.x, p.y, textureMapTextureLength, textureMapTextureLength, 0, 0, (int?)64, (int?)64);
                info_text.AppendChild(new HTMLParagraphElement { InnerHTML = "<strong>Fog</strong><br/>Unknown terrain, move units next to this tile to reveal secrets hidden by mysterious fog" });
            }
            else if (obj is Terrain)
            {
                switch ((obj as Terrain).type)
                {
                    case (int)Terrain.Types.Land:
                        info_text.AppendChild(new HTMLParagraphElement { InnerHTML = @"<strong>Land</strong><br/>Basic terrain for all units and most resources. Most units and all cities have to placed on this terrain.<br />
                                                                                    Units req. [Walk] skill to move on this terrain" });
                        break;
                    case (int)Terrain.Types.Mountain:
                        info_text.AppendChild(new HTMLParagraphElement { InnerHTML = @"<strong>Mountain</strong><br/>Special type of 'Land' terrain, most resources cannot appear on this terrain. Only few units can safely move over highest mountains in the Realm.<br />
                                                                                      Units req. both [Walk] and [Climb] skills to move on this terrain" });
                        break;
                    case (int)Terrain.Types.Sea:
                        info_text.AppendChild(new HTMLParagraphElement { InnerHTML = @"<strong>Sea</strong><br/>Water, as you expect. Only [Fish] resource can appear there, no cities. Almost no units can stay here over turn. All units fight weaker when standing in this terrain.<br />
                                                                                    Units req. [Swim] skill to move on this terrain" });
                        break;
                }
            }
            else if (obj is Resource)
            {

                info_text.AppendChild(new HTMLParagraphElement { InnerHTML = @"<strong>Resource</strong><br/>Resource is something, that you can use to boost production in your cities.<br />
                                                                               Every upgraded resource gives 'prosperity' points to target city. City with enough
                                                                               prosperity grow, which makes their output production bigger." });
                if (!(obj as Resource).ready)
                    info_text.AppendChild(new HTMLButtonElement { InnerHTML = $"Rebuild ({(obj as Resource).cost})", OnClick = (ev) => { if (isMenuVisible) return; if (!(obj as Resource).Rebuild()) Announce("You cannot rebuild this resource!"); else { ClickTo(refreshX, refreshY, refreshCtrl, altWasPressed); DisplayPlayerInfo(); } } });
            }
            else if (obj is Unit)
            {
                Unit u = obj as Unit;

                info_text.AppendChild(new HTMLParagraphElement
                {
                    InnerHTML = $"HP: {u.hp} / {u.maxhp}<br />" +
                                                                               @"<strong>" + u.name + @"</strong><br/>This is unit (" + u.name + @"). Units can move, attack, capture cities and build recruit tents.<br />
                                                                               To capture a city, move any unit to enemy city. Wait there for a while, then select the city (ctrl+click)
                                                                               and click 'Capture' button. To move, just click any tile near this one. If you want to display info about tile without moving your unit there, alt+click target tile.<br /><br />
                                                                               This unit has following abilities:<br />" + u.getAbilitiesInfo()
                });
                if ((Game.game.Map[obj.x, obj.y, 0] as Terrain).type == (int)Terrain.Types.Land && u.owner == Game.game.players[Game.game.playerState] && Game.game.Map[u.x, u.y, 1] == null)
                    info_text.AppendChild(new HTMLButtonElement
                    {
                        InnerHTML = $"Build Recruit Tent ({RecruitTent.price})",
                        OnClick = (ev) =>
                        {
                            if (isMenuVisible)
                                return;

                            RecruitTent r = RecruitTent.Build(Game.game.players[Game.game.playerState], obj.x, obj.y);
                            if (r == null)
                            {
                                Announce("Recruit Tent cannot be built");
                            }
                            else
                            {
                                Game.game.Map[obj.x, obj.y, 1] = r;
                                DisplayInfoAbout(obj, currentPlayer, refreshX, refreshY, refreshCtrl, altWasPressed);
                                DisplayPlayerInfo();
                                Draw(true, r.x, r.y);
                            }
                        }
                    });
                selectedUnit = u;
                DrawPossibleUnitMoves(selectedUnit);
            }
            else if (obj is City)
            {
                City c = obj as City;
                info_text.AppendChild(new HTMLParagraphElement { InnerHTML = @"<strong>City</strong><br/>This is the most important thing in this game. Cities can be captured by enemy units. <br />
                                                                               This city gives its owner " + c.Production + @" production each turn and allows him
                                                                               to recruit new units here. City also heals all friendly units inside by 2HP per turn." });
                if (c.CanCapture())
                    info_text.AppendChild(new HTMLButtonElement { InnerHTML = "Capture", OnClick = (ev) => { if (isMenuVisible) return; if (!c.Capture()) Announce("You cannot capture this city"); else ClickTo(refreshX, refreshY, refreshCtrl, altWasPressed); } });
                if (c.owner == Game.game.players[Game.game.playerState] && Game.game.Map[c.x, c.y, 2] == null)
                    info_text.AppendChild(new HTMLButtonElement { InnerHTML = "Recruit", OnClick = (ev) => { Recruit(-1, c.x, c.y); } });
                if (c.owner == Game.game.players[Game.game.playerState])
                    info_text.AppendChild(new HTMLButtonElement { InnerHTML = $"{(c.currentlyBuilding == null ? "Build" : $"Building {c.currentlyBuilding.name} ({c.currentlyBuilding.turnsRemaining})")}", OnClick = (ev) => { BuildSomething(c); } });
            }
            else if (obj is RecruitTent)
            {
                RecruitTent r = obj as RecruitTent;
                info_text.AppendChild(new HTMLParagraphElement { InnerHTML = @"<strong>Recruit Tent</strong><br/>Recruit Tent is building, that can be build by any unit for " + RecruitTent.price + @" resources. Here, you can recruit new units and heal them. <br />
                                                                               Recruit Tent heals units for 1 HP per turn. Using Recruit Tents, you can recruit units anywhere on the battlefield
                                                                               for 150% of the original price.<br />
                                                                               When any enemy unit moves to Recruit Tent, it is automatically destroyed and enemy who destroyed the tent gets 4 resources." });
                if (r.owner == Game.game.players[Game.game.playerState] && Game.game.Map[r.x, r.y, 2] == null)
                    info_text.AppendChild(new HTMLButtonElement { InnerHTML = "Recruit", OnClick = (ev) => { Recruit(-1, r.x, r.y, true); } });
            }
        }

        private static void AnimSteps(int x, int y, Path p, int a, MapObject obj, Player currentPlayer, int refreshX, int refreshY, bool refreshCtrl, bool altWasPressed)
        {
            Script.Call("console.log", p.pth);
            Bridge.Script.Call("console.log", $"Anim({x}, {y}, {p.pth[a].x}, {p.pth[a].y}, selectedUnit");
            Anim(x, y, p.pth[a].x, p.pth[a].y, selectedUnit, () =>
            {
                for (int i = obj.x - 1; i <= obj.x + 1; i++)
                {
                    if (i < 0 || i >= tiles)
                        continue;
                    for (int j = obj.y - 1; j <= obj.y + 1; j++)
                    {
                        if (j < 0 || j >= tiles)
                            continue;
                        Draw(true, i, j);
                    }
                }
                if (selectedUnit.turns > 0 || selectedUnit.canAttackToSomebody())
                {
                    DrawPossibleUnitMoves(selectedUnit);
                    DisplayInfoAbout(obj, currentPlayer, refreshX, refreshY, refreshCtrl, altWasPressed);
                }
                else
                {
                    selectedUnit = null; Draw();
                }
                if (a < p.pth.Count - 1)
                    AnimSteps(p.pth[a].x, p.pth[a].y, p, a + 1, obj, currentPlayer, refreshX, refreshY, refreshCtrl, altWasPressed);
            });
        }

        public static void DisplayPlayerInfo()
        {
            HTMLDivElement d = Document.GetElementById("playerInfo") as HTMLDivElement;
            Player p = Game.game.players[Game.game.playerState];
            d.InnerHTML = ""; //                                                         Color player name                                                                                                                  How many resources will I get every turn?                                                                                                    Sum your city outputs                                                                                                                                                                                   Count Agriculture Centrum (that is a building) bonus output each turn (current: -2)
            d.AppendChild(new HTMLParagraphElement { InnerHTML = $"<span style='color: {(p.color == 0 ? "blue" : (p.color == 1 ? "red" : (p.color == 2 ? "green" : "orange")))}'>{p.name}</span> ({p.resources} Resources) (+{Game.game.Map.OfType<MapObject>().Where(x => x is City).Select(x => x as City).Where(x => x.owner == Game.game.players[Game.game.playerState]).Select(x => x.Production).Sum() + Game.game.Map.OfType<MapObject>().Where(a => a is City).Select(a => a as City).Where(a => a.owner == Game.game.players[Game.game.playerState] && a.buildings[(int)Building.EBuildings.AgrCentrum].state == Building.States.Builded).Select(a => a.buildings[(int)Building.EBuildings.AgrCentrum].bonusOutputEachTurn).Sum()})" });

            // Update research button
            HTMLButtonElement but = Document.GetElementById("research") as HTMLButtonElement;
            but.InnerHTML = p.currentlyResearching == null ? "Research" : $"Researching {p.currentlyResearching.name} ({p.currentlyResearching.turnsRemaining})";
        }

        private static void Recruit(int unitId, int x, int y, bool isRecruitTent = false)
        {
            if (isMenuVisible)
                return;

            // Display recruit menu
            if (unitId == -1)
            {
                CloseRecruitBuildResearch();
                HTMLDivElement recruitBox = new HTMLDivElement { InnerHTML = $"<h1>Recruit</h1><p>You have {Game.game.players[Game.game.playerState].resources} resources</p>", Id = "recruitBox" };
                recruitBox.SetAttribute("class", "recruitBox");
                HTMLButtonElement recruitButCancel = new HTMLButtonElement { InnerHTML = "X", OnClick = (ev) => { Document.GetElementsByClassName("recruitBox").Last().Remove(); } };
                recruitButCancel.SetAttribute("class", "recruitStornoBut");
                recruitBox.AppendChild(recruitButCancel);
                HTMLDivElement wrapper = new HTMLDivElement();
                wrapper.SetAttribute("class", "wrapper");
                recruitBox.AppendChild(wrapper);

                int i = 0;
                foreach (Unit u in Unit.units)
                {
                    int j = i;
                    HTMLDivElement n = new HTMLDivElement();
                    n.AppendChild(new HTMLButtonElement { InnerHTML = $"{u.name} ({(int)(u.cost * (isRecruitTent ? 1.5 : 1))})", OnClick = (ev) => { Recruit(j, x, y, isRecruitTent); } });
                    wrapper.AppendChild(n);
                    i++;
                }

                Document.Body.AppendChild(recruitBox);
            }
            // Try to recruit
            else
            {
                Unit pattern = Unit.units[unitId];

                Player p = Game.game.players[Game.game.playerState];

                bool hasAllResearches = pattern.reqResearch.Count() == 0;
                if (!hasAllResearches)
                {
                    foreach (int i in pattern.reqResearch)
                    {
                        if (p.researches[i].state == Research.States.Researched)
                            hasAllResearches = true;
                        else
                        {
                            hasAllResearches = false;
                            break;
                        }
                    }
                }

                if (hasAllResearches && p.resources >= (int)(pattern.cost * (isRecruitTent ? 1.5 : 1)) && Game.game.Map[x, y, 2] == null && Game.game.players[Game.game.playerState] == (Game.game.Map[x, y, 1] as OwnerObject).owner)
                {
                    Textures t = Textures.unavalibeTile;
                    switch (Game.game.players[Game.game.playerState].color)
                    {
                        case 0:
                            t = Textures.unit_basic_blue;
                            break;
                        case 1:
                            t = Textures.unit_basic_red;
                            break;
                        case 2:
                            t = Textures.unit_basic_green;
                            break;
                        case 3:
                            t = Textures.unit_basic_yellow;
                            break;
                    }

                    p.resources -= (int)(pattern.cost * (isRecruitTent ? 1.5 : 1));
                    Unit u = Unit.getUnit(Game.game.players[Game.game.playerState], t, pattern, x, y);
                    if (Game.game.Map[x, y, 1] is City && (Game.game.Map[x, y, 1] as City).buildings[(int)Building.EBuildings.Barracks].state == Building.States.Builded)
                    {
                        u.hp += 5;
                        u.maxhp += 5;
                    }
                    Game.game.Map[x, y, 2] = u;

                    if (u.owner == Game.game.players[0])
                    {
                        Achievement.unitsRecruited++;
                        Achievement.Check();
                    }

                    DisplayPlayerInfo();
                    Document.GetElementsByClassName("recruitBox")[0].Remove();
                    Draw(true, x, y);
                }
                else
                {
                    if (!hasAllResearches)
                        Announce("This unit requires some researches. Research them and try it again");
                    else if (p.resources < (int)(pattern.cost * (isRecruitTent ? 1.5 : 1)))
                        Announce("You do not have enough resources required to build this unit");
                    else if (Game.game.players[Game.game.playerState] != (Game.game.Map[x, y, 1] as OwnerObject).owner)
                        Announce("You do not own this tile");
                    else
                        Announce("You cannot recruit this unit");
                }

            }
        }

        private static void CloseRecruitBuildResearch()
        {
            try
            {
                Document.GetElementById("researchDiv").Remove();
            }
            catch { }
            try
            {
                Document.GetElementById("recruitBox").Remove();
            }
            catch { }
        }

        private static void ResearchSomething(int id = -1)
        {
            if (isMenuVisible)
                return;

            Player p = Game.game.players[Game.game.playerState];
            if (id == -1)
            {
                CloseRecruitBuildResearch();
                HTMLDivElement outerDiv = new HTMLDivElement();
                outerDiv.SetAttribute("id", "researchDiv");
                outerDiv.InnerHTML = $"<button class='recruitStornoBut' onclick='document.getElementById(\"researchDiv\").remove();'>X</button><h2>Research</h2><p>Current level of science: {p.researchMultiplier}</p>{(p.currentlyResearching == null ? "" : $"<p>{p.currentlyResearching.name} <progress value={p.currentlyResearching.maximumTurnsRemaining - p.currentlyResearching.turnsRemaining} max={p.currentlyResearching.maximumTurnsRemaining}></progress></p>")}";

                HTMLDataListElement wrapper = new HTMLDataListElement();
                wrapper.SetAttribute("class", "techtreeWrapper");

                int i = 0;
                foreach (Research r in p.researches)
                {
                    string reqRes = "<br /><strong>Requirements:</strong>";
                    foreach (int q in r.reqResearch)
                    {
                        if (p.researches[q].state == Research.States.Researched)
                            continue;
                        reqRes += $"<br />{p.researches[q].name}";
                    }
                    reqRes += "<br />";
                    string scienceLevel = p.researchMultiplier >= r.reqScienceLevel ? "" : $"You need {r.reqScienceLevel} science level";

                    HTMLButtonElement but = new HTMLButtonElement();
                    int a = i++;
                    but.OnClick = (ev) => { ResearchSomething(a); };
                    if (r.state == Research.States.Researched)
                        but.InnerHTML = $"{r.name} (Researched)";
                    else if (r.state == Research.States.InProgress)
                        but.InnerHTML = $"{r.name} (Researching...)";
                    else
                        but.InnerHTML = $"{r.name} (Cost: {r.cost}, Turns: {r.turnsRemaining})";
                    HTMLDivElement d = new HTMLDivElement();
                    d.AppendChild(but);
                    HTMLDivElement div = new HTMLDivElement();
                    div.InnerHTML = $"{r.description}{reqRes}{scienceLevel}";
                    d.AppendChild(div);
                    wrapper.AppendChild(d);
                }

                outerDiv.AppendChild(wrapper);
                Document.Body.AppendChild(outerDiv);
            }
            else
            {
                if (p.researches[id].StartResearch(p))
                {
                    CloseRecruitBuildResearch();
                    //ResearchSomething();
                    DisplayPlayerInfo();
                }
            }
        }

        private static void BuildSomething(City c, int id = -1)
        {
            Player p = Game.game.players[Game.game.playerState];
            if (id == -1)
            {
                CloseRecruitBuildResearch();
                HTMLDivElement outerDiv = new HTMLDivElement();
                outerDiv.SetAttribute("class", "buildDiv");
                outerDiv.SetAttribute("id", "researchDiv");
                outerDiv.InnerHTML = $"<button class='recruitStornoBut' onclick='document.getElementById(\"researchDiv\").remove();'>X</button><h2>Build</h2><p>Resources: {p.resources}</p>{(c.currentlyBuilding == null ? "" : $"<p>{c.currentlyBuilding.name} <progress value={c.currentlyBuilding.maxTurnsRemaining - c.currentlyBuilding.turnsRemaining} max={c.currentlyBuilding.maxTurnsRemaining}></progress></p>")}";

                HTMLDataListElement wrapper = new HTMLDataListElement();
                wrapper.SetAttribute("class", "techtreeWrapper");

                int i = 0;
                foreach (Building b in c.buildings)
                {
                    string reqRes = "<br /><strong>Requirements:</strong>";
                    foreach (int q in b.researchReq)
                    {
                        if (p.researches[q].state == Research.States.Researched)
                            continue;
                        reqRes += $"<br />{p.researches[q].name}";
                    }
                    reqRes += "<br />";

                    HTMLButtonElement but = new HTMLButtonElement();
                    int a = i++;
                    but.OnClick = (ev) => { BuildSomething(c, a); };
                    if (b.state == Building.States.Builded)
                        but.InnerHTML = $"{b.name} (Built)";
                    else if (b.state == Building.States.InProgress)
                        but.InnerHTML = $"{b.name} (Building...)";
                    else
                        but.InnerHTML = $"{b.name} (Cost: {b.cost}, Turns: {b.turnsRemaining})";
                    HTMLDivElement d = new HTMLDivElement();
                    d.AppendChild(but);
                    HTMLDivElement div = new HTMLDivElement();
                    div.InnerHTML = $"{b.description}{reqRes}";
                    d.AppendChild(div);
                    wrapper.AppendChild(d);
                }

                outerDiv.AppendChild(wrapper);
                Document.Body.AppendChild(outerDiv);
            }
            else
            {
                if (c.buildings[id].StartBuilding(c))
                {
                    CloseRecruitBuildResearch();
                    //BuildSomething(c);
                    DisplayPlayerInfo();
                    // TODO: Refresh build button
                }
            }
        }

        private static string InterpolateRedGreenColor(int from, int to)
        {
            double green = 255 * (from / (double)to);
            return $"rgb({(int)(255 - green)}, {(int)green}, 0)";
        }

        private static HTMLDivElement announcement;
        private static double announceAlpha = 1;
        private static List<string> announceTexts = new List<string>();
        private static bool announceAnimIsRunning = false;
        public static bool isAnnounceEnabled = true;
        public static void Announce(string text, bool persistent = false)
        {
            // Dont display announces for AIs
            if (Game.game.players[Game.game.playerState].currAI != null)
                return;

            if (!isAnnounceEnabled)
                return;

            if (persistent)
            {
                if (debug)
                    Script.Call("console.log", $"Announce: {text} (persistent)");

                HTMLDivElement lightbox = new HTMLDivElement { ClassName = "lightbox" };
                HTMLDivElement lbContent = new HTMLDivElement { ClassName = "lightboxContent", InnerHTML = text };
                lightbox.AppendChild(lbContent);
                Document.Body.AppendChild(lightbox);
            }
            else
            {
                // TODO: Make this work
                //if (announceTexts.Last() != text)
                {
                    announceTexts.Add(text);
                    if (!announceAnimIsRunning)
                        RunAnnounceAnim();
                }
            }
        }
        private static void RunAnnounceAnim()
        {
            string text = announceTexts.First();

            announceAnimIsRunning = true;

            if (debug)
                Script.Call("console.log", $"Announce: {text} (non persistent)");

            announcement.InnerHTML = "";
            announcement.Style.SetProperty("display", "initial");
            HTMLParagraphElement p = new HTMLParagraphElement();
            p.InnerHTML = text;
            p.SetAttribute("style", "color: black");
            announcement.AppendChild(p);
            announcement.SetAttribute("style", "position:absolute;top: 10%;text-align: center;width: 100%;z-index: 100;oncontextmenu = \"return false;\";background-color: rgba(227, 227, 277, 1)");
            announceAlpha = 1;
            waitAnnounce = 0.5;

            announceTexts.Remove(text);
            Window.SetTimeout(AnnounceFrame, 15);
        }
        private static double waitAnnounce;
        public static void AnnounceFrame()
        {
            if (debug)
                Script.Call("console.log", $"Announce frame called with wait: {waitAnnounce} and alpha: {announceAlpha}");

            if (waitAnnounce > 0)
                waitAnnounce -= 0.005;
            else
                announceAlpha -= 0.03;
            announcement.SetAttribute("style", $"position:absolute;top: 10%;text-align: center;width: 100%;z-index: 100;oncontextmenu = \"return false;\";color: rgba(0, 0, 0, {announceAlpha});background-color: rgba(227, 227, 277, {announceAlpha})");
            if (announceAlpha > 0)
                Window.SetTimeout(AnnounceFrame, 15);
            else
            {
                announceAnimIsRunning = false;

                if (announceTexts.Count > 0)
                    RunAnnounceAnim();
                else
                    announcement.Style.SetProperty("display", "none");
            }
        }
    }
}