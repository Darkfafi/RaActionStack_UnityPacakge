using RaCollection;
using System;
using System.Collections.Generic;

namespace RaActionsStack
{
	public class RaAction : IDisposable
	{
		public delegate void Handler(RaAction action);
		private readonly RaElementCollection<RaActionData> _data = new RaElementCollection<RaActionData>();
		private readonly HashSet<string> _tags = new HashSet<string>();

		private Handler _method;
		private bool _canBeCancelled = false;

		public bool CanResolve => ActionState == State.None;
		public bool CanBeCancelled => ActionState == State.None && _canBeCancelled;

		public State ActionState
		{
			get; private set;
		}

		public static RaAction Create(Handler method, bool canBeCancelled = true)
		{
			return new RaAction(method, canBeCancelled);
		}

		private RaAction(Handler method, bool canBeCancelled)
		{
			_method = method;
			_canBeCancelled = canBeCancelled;
		}

		public void Cancel()
		{
			ThrowIfNotNoneState(nameof(Cancel));

			if(!_canBeCancelled)
			{
				throw new InvalidOperationException("Action was marked as 'Can't be Cancelled'");
			}

			ActionState = State.Cancelled;
		}

		public void Resolve()
		{
			ThrowIfNotNoneState(nameof(Resolve));
			ActionState = State.Resolved;
			_method?.Invoke(this);
		}

		public bool HasTag(string tag) => _tags.Contains(tag);

		public bool HasData(string key) => _data.Contains(key);

		public RaAction SetTag(string tag)
		{
			ThrowIfDisposedState(nameof(SetTag));
			_tags.Add(tag);
			return this;
		}

		public RaAction RemoveTag(string tag)
		{
			ThrowIfDisposedState(nameof(SetTag));
			_tags.Remove(tag);
			return this;
		}

		public RaAction RemoveData(string id)
		{
			ThrowIfDisposedState(nameof(RemoveData));
			_data.Remove(id);
			return this;
		}

		public RaAction SetData(string id, object data)
		{
			ThrowIfDisposedState(nameof(SetData));
			_data.Add(RaActionData.Create(id, data));
			return this;
		}

		public T GetData<T>()
		{
			TryGetData(out T data);
			return data;
		}

		public bool TryGetData<T>(out T value)
		{
			if(_data.TryGetItem(out RaActionData data, (x) => x.Value is T))
			{
				value = (T)data.Value;
				return true;
			}

			value = default;
			return false;
		}

		public bool TryGetData<T>(string key, out T value)
		{
			if(_data.TryGetItem(key, out RaActionData data) && data.Value is T castedValue)
			{
				value = castedValue;
				return true;
			}
			value = default;
			return false;
		}

		public List<T> GetAllData<T>()
		{
			List<T> values = new List<T>();
			_data.ForEach((data, index) =>
			{
				if(data.Value is T castedValue)
				{
					values.Add(castedValue);
				}
			});
			return values;
		}

		public void Dispose()
		{
			if(ActionState == State.Disposed)
			{
				return;
			}

			_method = null;
			_data.Clear();
			_tags.Clear();
			ActionState = State.Disposed;
		}

		private void ThrowIfNotNoneState(string method)
		{
			if(ActionState != State.None)
			{
				throw new InvalidOperationException($"Can't perform {method} on consumed Action [{nameof(ActionState)}: {ActionState}]");
			}
		}

		private void ThrowIfDisposedState(string method)
		{
			if(ActionState == State.Disposed)
			{
				throw new InvalidOperationException($"Can't perform {method} on dispossed Action [{nameof(ActionState)}: {ActionState}]");
			}
		}

		public enum State
		{
			None = 0,
			Resolved = 1,
			Cancelled = 2,
			Disposed = 100,
		}
	}
}