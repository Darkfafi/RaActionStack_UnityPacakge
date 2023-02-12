using System;
using System.Collections.Generic;

namespace RaActionsStack
{
	public class RaActionStack : IDisposable
	{
		public event RaAction.Handler ActionPushedEvent;
		public event RaAction.Handler ActionPoppedEvent;
		public event RaAction.Handler ActionProcessingEvent;
		public event RaAction.Handler ActionResolvedEvent;
		public event RaAction.Handler ActionCancelledEvent;
		public event Action StartProcessingEvent;
		public event Action EndProcessingEvent;

		private Stack<RaAction> _stack = new Stack<RaAction>();
		private bool _isProcessing = false;

		public void Push(RaAction action)
		{
			_stack.Push(action);
			ActionPushedEvent?.Invoke(action);
			Process();
		}

		private void Process()
		{
			if(_isProcessing || _stack.Count == 0)
			{
				return;
			}

			_isProcessing = true;

			StartProcessingEvent?.Invoke();

			List<RaAction> processedActions = new List<RaAction>();

			while(_stack.Count > 0)
			{
				RaAction action = _stack.Peek();

				ActionProcessingEvent?.Invoke(action);

				// If the peek has changed, go to new peek first
				if(_stack.Peek() != action)
				{
					continue;
				}

				action = _stack.Pop();

				// When stack has stabalized, Resolve unresolved actions
				if(action.CanResolve)
				{
					action.Resolve();
				}

				// Send appropriate events on new Action State
				switch(action.ActionState)
				{
					case RaAction.State.Resolved:
						ActionResolvedEvent?.Invoke(action);
						break;
					case RaAction.State.Cancelled:
						ActionCancelledEvent?.Invoke(action);
						break;
				}

				ActionPoppedEvent?.Invoke(action);

				processedActions.Add(action);
			}

			for(int i = processedActions.Count - 1; i >= 0; i--)
			{
				processedActions[i].Dispose();
			}

			_isProcessing = false;

			EndProcessingEvent?.Invoke();
		}

		public void Dispose()
		{
			ActionPushedEvent = null;
			ActionPoppedEvent = null;
			ActionProcessingEvent = null;
			ActionResolvedEvent = null;
			ActionCancelledEvent = null;

			_isProcessing = default;

			while(_stack.Count > 0)
			{
				_stack.Pop().Dispose();
			}

			_stack = null;
		}
	}
}