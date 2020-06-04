using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Colyseus;
using UnityEngine;
using UnityEngine.Events;
using DataChange = Colyseus.Schema.DataChange;

public class OnInitialState : UnityEvent<GameRoomState> { };

public class OnGamePhaseChange : UnityEvent<string> { };

public class ColyseusRoom : GenericSingleton<ColyseusRoom>
{
    public UnityEvent onConnect;
    public UnityEvent onJoin;
    public OnInitialState onInitialState;
    public OnGamePhaseChange onGamePhaseChange;

    private const string RoomName = "game_room";

    private ColyseusRoom<GameRoomState> _room;
    private bool _initialStateReceived;

    public string SessionId => _room?.SessionId;

    public GameRoomState State => _room?.State;

    public bool Joined => _room != null && _room.colyseusConnection.IsOpen;

    const float TimeoutSeconds = 5f;
    static int reconnectTimes = 3;
    CancellationTokenSource reconnectCancellation = new CancellationTokenSource();

    public async void JoinRoom()
    {
        var client = GameManager.Instance.RoomController.Client;
        try
        {
            _room = await client.JoinOrCreate<GameRoomState>(RoomName);

            RegisterRoomEvent();
            RegisterRoomHandlers();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async void LeaveRoom()
    {
        await _room.Leave();
        _room = null;
    }

    public async void SendPlacement(int[] placement) => await _room.Send("place", new { placement });

    public async void SendTurn(int targetIndex) => await _room.Send("turn", new { targetIndex });

    // handlers

    public void OnOpenHandler()
    {
        onConnect?.Invoke();
    }

    private void RegisterRoomHandlers()
    {
        // TODO: can's use Room.OnJoin
        ConnectionHandler();
        OnJoinHandler();
        _room.OnStateChange += OnRoomStateChangeHandler;
    }

    private void ConnectionHandler()
    {
        _room.colyseusConnection.OnClose += closeCode =>
        {
            Debug.Log($"Connection OnClose {closeCode}");
            ScheduleReconnectRoom();
        };
        _room.colyseusConnection.OnError += errorMsg =>
        {
            Debug.Log($"Connection OnError {errorMsg}");
            ScheduleReconnectRoom();
        };
    }

    async void ScheduleReconnectRoom()
    {
        while (!reconnectCancellation.IsCancellationRequested)
        {
            Debug.Log($"Reconnect times {reconnectTimes}");
            if (reconnectTimes <= 0)
            {
                reconnectCancellation.Cancel();
                Debug.Log("Room connection failed or timed out.");
            }
            else
            {
                var client = GameManager.Instance.RoomController.Client;
                try
                {
                    _room = await client.Reconnect<GameRoomState>(_room.Id, _room.SessionId);

                    reconnectTimes = 3;
                    reconnectCancellation.Cancel();
                    RegisterRoomEvent();
                    RegisterRoomHandlers();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                Debug.Log($"Failed to connect to the server. Retry after 5 seconds...");
                reconnectTimes--;
                await Task.Delay(TimeSpan.FromSeconds(TimeoutSeconds), reconnectCancellation.Token);
            }
        }
    }

    private void OnJoinHandler()
    {
        _room.State.OnChange += OnStateChange;

        onJoin?.Invoke();
    }

    private void OnRoomStateChangeHandler(GameRoomState state, bool isFirstState)
    {
        if (isFirstState)
        {
            // First setup of your client state
            if (!_initialStateReceived)
            {
                _initialStateReceived = true;
                onInitialState?.Invoke(state);
            }
        }
        else
        {
            // Further updates on your client state
        }
    }

    private void OnStateChange(List<DataChange> dataChanges)
    {
        if (!_initialStateReceived) return;

        foreach (var change in dataChanges.Where(change => change.Field == "phase"))
        {
            onGamePhaseChange?.Invoke(change.Value.ToString());
        }
    }

    // events

    private void RegisterRoomEvent()
    {
        onInitialState = new OnInitialState();
        onGamePhaseChange = new OnGamePhaseChange();
    }
}