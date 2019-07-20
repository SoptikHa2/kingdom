using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bridge;
using Bridge.Html5;

namespace Kingdom
{
    class Pong
    {
        public static Pong pong;

        public HTMLCanvasElement canvas;
        public CanvasRenderingContext2D ctx;
        private bool ingame = false;
        public ulong score = 0;
        private ulong highscore = 0;
        private Ball ball;

        public int pX;
        public int pWidth;

        private HTMLParagraphElement gameInfo;

        public Pong()
        {
            canvas = Document.GetElementById("pong") as HTMLCanvasElement;
            ctx = canvas.GetContext("2d").As<CanvasRenderingContext2D>();

            gameInfo = Document.GetElementById("gameInfo") as HTMLParagraphElement;

            try
            {
                highscore = ulong.Parse(Window.LocalStorage["highscore"].ToString());
            }
            catch { }

            pong = this;

            canvas.OnClick = (ev) => { if (!ingame) { Achievement.achievements[(int)Achievement.eAchievs.Upgrade].isSpecial = false; Achievement.Check(); pWidth = 200; pX = ev.ClientX - 250; ingame = true; score = 0; ball = new Ball { x = canvas.OffsetWidth / 2, y = canvas.OffsetHeight / 2, vx = new double[] { -0.5, -0.4, -0.3, -0.2, -0.1, 0.1, 0.2, 0.3, 0.4, 0.5 }[new Random().Next(10)], vy = 5 }; ball.Move(); } };
            Document.Body.OnMouseMove = (ev) =>
            {
                pX = ev.ClientX - 90 - canvas.OffsetLeft;
                if (pX < 0)
                    pX = 0;
                if (pX + pWidth > canvas.OffsetWidth)
                    pX = canvas.OffsetWidth - pWidth;
            };
            // Mobile devices
            /*
            canvas.OnTouchMove = (ev) =>
            {
                pX = ev.PageX - 250;
                Console.WriteLine(ev.PageX - 250);
                if (pX < 0)
                    pX = 0;
                if (pX + pWidth > canvas.OffsetWidth)
                    pX = canvas.OffsetWidth - pWidth;
            };*/

            gameInfo.InnerHTML = $"Score: {score}<br />Highscore: {highscore}";
        }

        public void Reset()
        {
            ctx.ClearRect(0, 0, canvas.OffsetWidth, canvas.OffsetHeight);
            ingame = false;
            if (score > highscore)
                highscore = score;
            ball = null;

            gameInfo.InnerHTML = $"Score: {score}<br />Highscore: {highscore}";
            Window.LocalStorage["highscore"] = highscore;
        }

        public void Draw()
        {
            ctx.ClearRect(0, 0, canvas.OffsetWidth, canvas.OffsetHeight);
            ctx.FillRect((int)ball.x, (int)ball.y, Ball.width, Ball.height);

            ctx.FillRect(pong.pX, canvas.OffsetWidth / 100 * 95, pong.pWidth, canvas.OffsetWidth / 100 * 2);

            gameInfo.InnerHTML = $"Score: {score}<br />Highscore: {highscore}";
        }
    }

    internal class Ball
    {
        public double vx, vy, x, y;
        public const int width = 10, height = 10;
        private const double gravity = 0;
        private const int maxVx = 12;
        private const int maxVy = 15;
        private int k = 1;

        public void Move()
        {
            if (vx > maxVx)
            {
                vx = maxVx;
                if (Pong.pong.pWidth > 80)
                    Pong.pong.pWidth -= 2;
            }
            if (vx < -maxVx)
            {
                vx = -maxVx;
                if (Pong.pong.pWidth > 80)
                    Pong.pong.pWidth -= 2;
            }
            if (vy > maxVy)
            {
                vy = maxVy;
                if (Pong.pong.pWidth > 80)
                    Pong.pong.pWidth -= 2;
            }
            if (vy < -maxVy)
            {
                vy = -maxVy;
                if (Pong.pong.pWidth > 80)
                    Pong.pong.pWidth -= 2;
            }

            x += vx;
            y += vy;
            vy += gravity;

            if (y + height > Pong.pong.canvas.OffsetHeight)
            {
                Pong.pong.Reset();
                return;
            }
            if (x < 0)
            {
                x = 0;
                vx = -vx;
                k++;
            }
            if (x + width > Pong.pong.canvas.OffsetWidth)
            {
                x = Pong.pong.canvas.OffsetWidth - width;
                vx = -vx;
                k++;
            }
            if (y < 0)
            {
                y = 0;
                vy = -vy;
                k++;
            }
            if (k > 0 && y >= Pong.pong.canvas.OffsetWidth / 100 * 95 && y <= Pong.pong.canvas.OffsetWidth / 100 * 99 && x >= Pong.pong.pX && x <= Pong.pong.pX + Pong.pong.pWidth)
            {
                k = 0;
                y = Pong.pong.canvas.OffsetWidth / 100 * 93;
                vy = -vy - (vy * vy < 10 ? 0.2 : 0.7);
                if ((vx > 0 && (x >= Pong.pong.pX + (Pong.pong.pWidth / 2d))) || (vx <= 0 && (x <= Pong.pong.pX + (Pong.pong.pWidth / 2d))))
                {
                    vx = vx + (vx * 0.3);
                    Pong.pong.score += 40;
                }
                else
                {
                    vx = -vx - (vx * 0.15);
                    Pong.pong.score += 20;
                }
            }

            Pong.pong.Draw();

            Window.RequestAnimationFrame(Move);
        }
    }
}
