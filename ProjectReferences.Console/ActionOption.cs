using System;
using Mono.Options;

namespace ProjectReferences.App
{
    sealed class ActionOption : Option
    {
        public bool Called { get; private set; }

        public ActionOption(string prototype, string description, Action<string> action, bool hidden = false)
            : this(prototype, description, 0, action, hidden)
        {
        }

        public ActionOption(string prototype, string description, int count, Action<string> action, bool hidden = false)
            : base(prototype, description, count, hidden)
        {
            _action = action ?? throw new ArgumentNullException("action");
        }

        protected override void OnParseComplete(OptionContext c)
        {
            if (MaxValueCount == 0)
            {
                _action(c.OptionName);
            }
            else if (MaxValueCount == 1)
            {
                _action(c.OptionValues[0]);
            }

            Called = true;
        }

        private readonly Action<string> _action;
    }
}
