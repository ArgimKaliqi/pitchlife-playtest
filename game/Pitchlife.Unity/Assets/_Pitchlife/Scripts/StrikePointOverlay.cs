using UnityEngine;

namespace Pitchlife.P01
{
    public static class StrikePointOverlay
    {
        public static Rect BallRect(float width, float height, float scale)
        {
            float w = width / scale, h = height / scale;
            float diameter = Mathf.Min(w * .68f, h * .52f, 550);
            return new Rect((w - diameter) / 2, h * .49f - diameter / 2, diameter, diameter);
        }

        public static bool TryContact(Vector2 screenPosition, float width, float height, float scale, out Vector2 contact)
        {
            Rect ball = BallRect(width, height, scale);
            Vector2 gui = new Vector2(screenPosition.x, height - screenPosition.y) / scale;
            contact = new Vector2((gui.x - ball.center.x) / (ball.width / 2),
                (ball.center.y - gui.y) / (ball.height / 2));
            return StrikePhysics.ValidContact(contact);
        }

        public static bool Draw(float width, float height, float scale, Vector2 hover, bool hovering, float speed)
        {
            float w = width / scale, h = height / scale;
            Rect ball = BallRect(width, height, scale);
            Color old = GUI.color;
            GUI.color = new Color(.025f, .055f, .07f, .97f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            var title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32, fontStyle = FontStyle.Bold };
            var text = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 21, wordWrap = true };
            GUI.color = new Color(.6f, 1, .78f);
            GUI.Label(new Rect(20, h * .08f, w - 40, 48), "CHOOSE YOUR STRIKE", title);
            GUI.color = Color.white;
            GUI.Label(new Rect(20, h * .08f + 52, w - 40, 65),
                "Aim and power locked  •  " + speed.ToString("0.0") + " m/s\nClick or tap where your foot meets the ball", text);

            GUI.color = new Color(0, 0, 0, .35f);
            GUI.DrawTexture(new Rect(ball.x + 12, ball.y + 20, ball.width, ball.height), FootballBallVisual.Dot);
            GUI.color = Color.white;
            GUI.DrawTexture(ball, FootballBallVisual.Closeup);
            GUI.color = new Color(.15f, .7f, .5f, .25f);
            GUI.DrawTexture(new Rect(ball.center.x - 1, ball.y + 10, 2, ball.height - 20), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(ball.x + 10, ball.center.y - 1, ball.width - 20, 2), Texture2D.whiteTexture);

            Vector2 point = hovering ? hover : Vector2.zero;
            Vector2 marker = new Vector2(ball.center.x + point.x * ball.width / 2,
                ball.center.y - point.y * ball.height / 2);
            GUI.color = new Color(0, .15f, .1f);
            GUI.DrawTexture(new Rect(marker.x - 12, marker.y - 12, 24, 24), FootballBallVisual.Dot);
            GUI.color = new Color(.6f, 1, .45f);
            GUI.DrawTexture(new Rect(marker.x - 7, marker.y - 7, 14, 14), FootballBallVisual.Dot);
            GUI.color = Color.white;
            GUI.Label(new Rect(20, ball.yMax + 20, w - 40, 60), StrikePhysics.Describe(point), text);
            GUI.color = new Color(.75f, .83f, .85f);
            GUI.Label(new Rect(25, ball.yMax + 80, w - 50, 65),
                "Center: straight   •   Low: chip\nLeft / right: curve   •   High: lower, dipping shot", text);
            GUI.color = Color.white;
            bool cancel = GUI.Button(new Rect(w / 2 - 130, h - 100, 260, 48), "Back to aiming");
            GUI.color = old;
            return cancel;
        }
    }
}
