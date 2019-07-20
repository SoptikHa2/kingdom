using Bridge.Html5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    class Achievement
    {
        private static char separator = (char)10000;

        public static int enemiesKilled, unitsLost, finishedGames, citiesCaptured, resourcesRebuilt, unitsRecruited, researchesResearched, recruitTentsBuilt, recruitTentsDestroyed;

        public static void Load(string forceLoadString = null)
        {
            string s = (forceLoadString ?? Window.LocalStorage["kingdom_achievs"] ?? $"0{separator}0{separator}0{separator}0{separator}0{separator}0{separator}0{separator}0{separator}0").ToString();
            try
            {
                int[] numbers = s.Split(separator).Select(x => int.Parse(x)).ToArray();
                enemiesKilled = numbers[0];
                unitsLost = numbers[1];
                finishedGames = numbers[2];
                citiesCaptured = numbers[3];
                resourcesRebuilt = numbers[4];
                unitsRecruited = numbers[5];
                researchesResearched = numbers[6];
                recruitTentsBuilt = numbers[7];
                recruitTentsDestroyed = numbers[8];

                Check(false);
            }
            catch
            {
                Load($"0{separator}0{separator}0{separator}0{separator}0{separator}0{separator}0{separator}0{separator}0");
            }

            s = (Window.LocalStorage["kingdom_achievs_complete"] ?? "").ToString();
            try
            {
                int[] achievsCompleted = s.Split(separator).Select(x => int.Parse(x)).ToArray();
                foreach (int i in achievsCompleted)
                    achievements[i].isDone = true;
            }
            catch { }
        }

        public static void Save()
        {
            string s = $"{enemiesKilled}{separator}{unitsLost}{separator}{finishedGames}{separator}{citiesCaptured}{separator}{resourcesRebuilt}{separator}{researchesResearched}" +
                $"{separator}{recruitTentsBuilt}{separator}{recruitTentsDestroyed}";
            Window.LocalStorage["kingdom_achievs"] = s;

            s = "";
            int completedAchievs = achievements.Where(x => x.isDone).Count();
            int ca = 0;
            for (int i = 0; i < achievements.Length; i++)
            {
                if (!achievements[i].isDone)
                    continue;
                s += i;
                if (++ca != completedAchievs)
                    s += separator;
            }
            Window.LocalStorage["kingdom_achievs_complete"] = s;
        }

        public enum eAchievs
        {
            DestroyThemAll,
            GameOver,
            YouShallNotPass,
            Ironman,
            PowerOfGunpowder,
            Upgrade
        }

        public static Achievement[] achievements =
        {
            new Achievement("Destroy Them All", "Kill 25 enemies", 25, 0, 0, 0, 0, 0, 0, 0, 0, false, false),
            new Achievement("Game Over", "Make it to the end", 0, 0, 1, 0, 0, 0, 0, 0, 0, false, false),
            new Achievement("You shall not pass", "Kill 10 enemies while defending in one game", 0, 0, 0, 0, 0, 0, 0, 0, 0, true, false),
            new Achievement("I'm Ironman!", "Research 'Ironsmithing'", 0, 0, 0, 0, 0, 0, 0, 0, 0, true, false),
            new Achievement("The power of gunpowder", "Research 'Gunpowder'", 0, 0, 0, 0, 0, 0, 0, 0, 0, true, false),

            new Achievement("What about an upgrade?", "You'll need higher resolution to play this game", 0, 0, 0, 0, 0, 0, 0, 0, 0, true, true)
        };

        public string name, description;
        public int reqEnemiesKilled, reqUnitsLost, reqFinishedGames, reqCitiesCaptured, reqResourcesRebuilt, reqUnitsRecruited, reqResearchesResearched, reqRecruitTentsBuilt, reqRecruitTentsDestroyed;
        public bool isSpecial, isDone, isSecret;

        private Achievement(string name, string description, int reqEnemiesKilled, int reqUnitsLost, int reqFinishedGames, int reqCitiesCaptured, int reqResourcesRebuilt, int reqUnitsRecruited, int reqResearchesResearched, int reqRecruitTentsBuilt, int reqRecruitTentsDestroyed, bool isSpecial, bool isSecret)
        {
            this.name = name;
            this.description = description;
            this.reqEnemiesKilled = reqEnemiesKilled;
            this.reqUnitsLost = reqUnitsLost;
            this.reqFinishedGames = reqFinishedGames;
            this.reqCitiesCaptured = reqCitiesCaptured;
            this.reqResourcesRebuilt = reqResourcesRebuilt;
            this.reqUnitsRecruited = reqUnitsRecruited;
            this.reqResearchesResearched = reqResearchesResearched;
            this.reqRecruitTentsBuilt = reqRecruitTentsBuilt;
            this.reqRecruitTentsDestroyed = reqRecruitTentsDestroyed;
            this.isSecret = isSecret;
            this.isSpecial = isSpecial;
            this.isDone = false;
        }

        private void CheckAchiev()
        {
            if (!isSpecial && !isDone)
                if (enemiesKilled >= reqEnemiesKilled && unitsLost >= reqUnitsLost && finishedGames >= reqFinishedGames && citiesCaptured >= reqCitiesCaptured &&
                    resourcesRebuilt >= reqResourcesRebuilt && unitsRecruited >= reqUnitsRecruited && researchesResearched >= reqResearchesResearched &&
                    recruitTentsBuilt >= reqRecruitTentsBuilt && recruitTentsDestroyed >= reqRecruitTentsDestroyed)
                {
                    isDone = true;
                    App.Announce("Achieved new achievement: " + name);
                }
        }

        public static void Check(bool save = true)
        {
            for (int i = 0; i < achievements.Length; i++)
                achievements[i].CheckAchiev();
            if (save)
                Save();
        }

        public static string getAchievHTML()
        {
            string s = "";
            for (int i = 0; i < achievements.Length; i++)
            {
                Achievement a = achievements[i];
                s += $"<div class='achiev{(a.isDone ? "Done" : "")}'><h3>{a.name}</h3><p>{((a.isSecret && !a.isDone) ? "<i>This achievement is a big secret. You have to unlock it by accident.</i>" : a.description)}</p></div>";
            }
            return s;
        }
    }
}
