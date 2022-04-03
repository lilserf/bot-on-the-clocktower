namespace Bot.DSharp
{
    public class DiscordWrapper<T> where T : notnull
    {
        public T Wrapped { get; }
        public DiscordWrapper(T wrapped)
        {
            Wrapped = wrapped;
		}

		public override bool Equals(object? other)
		{
			if (other is DiscordWrapper<T> obj)
			{
				return Wrapped.Equals(obj.Wrapped);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Wrapped.GetHashCode();
		}
	}
}
