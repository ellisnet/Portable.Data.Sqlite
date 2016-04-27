using System;

namespace Portable.Data
{
    public delegate void StateChangeEventHandler(object sender, StateChangeEventArgs e);

    public sealed class StateChangeEventArgs : EventArgs
    {
        private readonly ConnectionState _currentState;
        private readonly ConnectionState _originalState;

        public StateChangeEventArgs(ConnectionState originalState, ConnectionState currentState)
        {
            _originalState = originalState;
            _currentState = currentState;
        }

        public ConnectionState CurrentState 
        {
            get { return _currentState; }
        }

        public ConnectionState OriginalState 
        {
            get { return _originalState; }
        }
    }
}
