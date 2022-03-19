namespace PlayerXP
{
	public class PlayerInfo
	{
		public string Name;
		public int Level;
		public int XP;
		public float Karma;

		public PlayerInfo(string name)
		{
			this.Name = name;
			Level = PlayerXP.instance.Config.LevelInitial;
			XP = PlayerXP.instance.Config.XpInitial;
			Karma = PlayerXP.instance.Config.KarmaInitial;
		}
	}
}
