using System.Collections.Generic;
using System.Linq;
using Colyseus.Schema;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal enum GameMode
{
    Placement,
    Battle,
    Result
}

internal struct GamePhase
{
    public const string Waiting = "waiting";
    public const string Place = "place";
    public const string Battle = "battle";
    public const string Result = "result";
}

public class GameSceneController : MonoBehaviour
{
    public static GameSceneController Instance;

    public MapView mapView;

    public Text message;

    public Button rotateShipButton;

    public Button leaveButton;

    private const int MapSize = 10;

    private bool PlaceShipHorizontally
    {
        get => _placeShipHorizontally;
        set
        {
            _placeShipHorizontally = value;
            UpdateCursor();
        }
    }

    private GameMode _mode;

    private int _shipPlaced;

    private bool _placeShipHorizontally;

    private int _cellCount;

    private int[] _placement;

    private RoomController _roomController;
    private ColyseusRoom _room;
    private GameRoomState _state;
    private int _myPlayerNumber;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        rotateShipButton.onClick.AddListener(RotateShip);
        leaveButton.onClick.AddListener(Leave);
    }

    private void Start()
    {
        _roomController = GameManager.Instance.RoomController;
        _room = ColyseusRoom.Instance;

        if (_roomController.Client == null)
        {
            SceneManager.LoadScene("ConnectingScene");
            return;
        }

        _cellCount = MapSize * MapSize;
        _placement = new int[_cellCount];

        for (var i = 0; i < _cellCount; i++)
        {
            _placement[i] = 0; // empty
        }

        rotateShipButton.gameObject.SetActive(_room.Joined);
        leaveButton.gameObject.SetActive(!_room.Joined);

        _room.onInitialState.AddListener(InitialStateHandler);
        _room.onGamePhaseChange.AddListener(GamePhaseChangeHandler);

        if (_room.State != null)
        {
            InitialStateHandler(_room.State);
        }
    }

    private void OnDestroy()
    {
        if (_room == null) return;
        _room.onInitialState.RemoveListener(InitialStateHandler);
        _room.onGamePhaseChange.RemoveListener(GamePhaseChangeHandler);
    }

    private void OnApplicationQuit()
    {
        Leave();
    }

    // Game Server Events
    // Game Phase Changes
    // waiting > placing > battle > result

    private void BeginShipPlacement()
    {
        _mode = GameMode.Placement;
        message.text = "Place your ships";
        _shipPlaced = 0;
        mapView.SetPlacementMode();
        UpdateCursor();
    }

    private void WaitForOpponentToPlace()
    {
        _mode = GameMode.Placement;
        mapView.SetDisabled();
        message.text = "Waiting for opponent to place ships";
    }

    private void WaitForOpponentTurn()
    {
        _mode = GameMode.Battle;
        mapView.SetDisabled();
        message.text = "Waiting for opponent to attack";
    }

    private void StartTurn()
    {
        _mode = GameMode.Battle;
        mapView.SetAttackMode();
        message.text = "Take your turn!";
    }

    private void ShowResult()
    {
        _mode = GameMode.Result;
        message.text = _state.winningPlayer == _myPlayerNumber ? "You win!" : "You lost :(";
        mapView.SetDisabled();
        rotateShipButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(true);
    }

    // UI events

    public void PlaceShip(Vector3Int coordinate)
    {
        if (_mode != GameMode.Placement) return;

        int size;
        ShipType shipType;

        switch (_shipPlaced)
        {
            case 0:
                size = 2;
                shipType = ShipType.Destroyer;
                break;
            case 1:
                size = 3;
                shipType = ShipType.Cruiser;
                break;
            case 2:
                size = 3;
                shipType = ShipType.Submarine;
                break;
            case 3:
                size = 4;
                shipType = ShipType.BattleShip;
                break;
            case 4:
                size = 5;
                shipType = ShipType.AircraftCarrier;
                break;
            default:
                return;
        }

        var shipWidth = PlaceShipHorizontally ? size : 1;
        var shipHeight = PlaceShipHorizontally ? 1 : size;

        // check map bounds
        if (coordinate.x < 0 || coordinate.x + (shipWidth - 1) > MapSize || coordinate.y - (shipHeight - 1) < 0)
        {
            return;
        }

        for (var i = 0; i < size; i++)
        {
            if (PlaceShipHorizontally)
            {
                var checkCoordinate = coordinate + new Vector3Int(i, 0, 0);
                if (!SetPlacementCell(checkCoordinate, shipType, true)) return;
            }
            else
            {
                var checkCoordinate = coordinate + new Vector3Int(0, -i, 0);
                if (!SetPlacementCell(checkCoordinate, shipType, true)) return;
            }
        }

        for (var i = 0; i < size; i++)
        {
            if (PlaceShipHorizontally)
            {
                SetPlacementCell(coordinate + new Vector3Int(i, 0, 0), shipType);
            }
            else
            {
                SetPlacementCell(coordinate + new Vector3Int(0, -i, 0), shipType);
            }
        }

        mapView.SetShip(shipType, coordinate, PlaceShipHorizontally);
        _shipPlaced++;
        UpdateCursor();

        if (_shipPlaced == 5)
        {
            _room.SendPlacement(_placement);
            WaitForOpponentToPlace();
        }
    }

    private void RotateShip()
    {
        PlaceShipHorizontally = !PlaceShipHorizontally;
    }

    public void TakeTurn(Vector3Int coordinate)
    {
        var targetIndex = coordinate.y * MapSize + coordinate.x;

        _room.SendTurn(targetIndex);
    }

    // Private

    private void UpdateCursor()
    {
        switch (_shipPlaced)
        {
            case 0:
                mapView.SetShipCursor(ShipType.Destroyer, PlaceShipHorizontally);
                break;
            case 1:
                mapView.SetShipCursor(ShipType.Cruiser, PlaceShipHorizontally);
                break;
            case 2:
                mapView.SetShipCursor(ShipType.Submarine, PlaceShipHorizontally);
                break;
            case 3:
                mapView.SetShipCursor(ShipType.BattleShip, PlaceShipHorizontally);
                break;
            case 4:
                mapView.SetShipCursor(ShipType.AircraftCarrier, PlaceShipHorizontally);
                break;
        }
    }

    private bool SetPlacementCell(Vector3Int coordinate, ShipType shipType, bool testOnly = false)
    {
        var cellIndex = coordinate.y * MapSize + coordinate.x;

        if (cellIndex < 0 || cellIndex >= _cellCount) return false;
        if (_placement[cellIndex] > 0) return false;
        if (testOnly) return true;

        _placement[cellIndex] = shipType.GetHashCode();

        return true;
    }

    // networking

    private void InitialStateHandler(GameRoomState initialState)
    {
        _state = initialState;

        var me = _state.players[_room.SessionId];

        _myPlayerNumber = me?.seat ?? -1;

        _state.OnChange += StateChangeHandler;
        _state.player1Shots.OnChange += ShotChangedPlayer1;
        _state.player2Shots.OnChange += ShotChangedPlayer2;

        GamePhaseChangeHandler(_state.phase);
    }

    private void StateChangeHandler(List<DataChange> dataChanges)
    {
        foreach (var unused in dataChanges.Where(change => change.Field == "playerTurn"))
        {
            CheckTurn();
        }
    }

    private void ShotChangedPlayer1(int key, short value)
    {
        var marker = value == 1 ? Marker.Hit : Marker.Miss;
        mapView.SetMarker(key, marker, _myPlayerNumber == 1);
    }

    private void ShotChangedPlayer2(int key, short value)
    {
        var marker = value == 1 ? Marker.Hit : Marker.Miss;
        mapView.SetMarker(key, marker, _myPlayerNumber == 2);
    }

    private void CheckTurn()
    {
        if (_state.playerTurn == _myPlayerNumber)
        {
            StartTurn();
        }
        else
        {
            WaitForOpponentTurn();
        }
    }

    private void Leave()
    {
        _room.LeaveRoom();
        SceneManager.LoadScene("ConnectingScene");
    }

    private void GamePhaseChangeHandler(string phase)
    {
        switch (phase)
        {
            case GamePhase.Waiting:
                Leave();
                break;
            case GamePhase.Place:
                BeginShipPlacement();
                break;
            case GamePhase.Battle:
                CheckTurn();
                break;
            case GamePhase.Result:
                ShowResult();
                break;
        }
    }
}