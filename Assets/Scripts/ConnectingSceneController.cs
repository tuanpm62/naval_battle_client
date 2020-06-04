using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectingSceneController : MonoBehaviour
{
    public Text message;

    private RoomController _roomController;

    private ColyseusRoom _room;

    private void Start()
    {
        message.text = "Connecting...";

        _roomController = GameManager.Instance.RoomController;
        _room = ColyseusRoom.Instance;

        _room.onConnect.AddListener(OnConnect);
        _room.onJoin.AddListener(OnJoin);

        if (_roomController.Client == null)
        {
            GameManager.Instance.InitializeClient();
        }
        else
        {
            OnConnect();
        }
    }

    private void OnApplicationQuit()
    {
        _room.LeaveRoom();
    }

    private void OnConnect()
    {
        message.text = "Finding a game...";

        if (!_room.Joined)
        {
            _room.JoinRoom();
        }
        else
        {
            OnJoin();
        }
    }

    private void OnJoin()
    {
        message.text = "Joined! Finding another player...";

        _room.onGamePhaseChange.AddListener(GamePhaseChangeHandler);
    }

    private static void GamePhaseChangeHandler(string phase)
    {
        if (phase == GamePhase.Place)
        {
            SceneManager.LoadScene("GameScene");
        }
    }

    private void OnDestroy()
    {
        if (_room == null) return;
        _room.onConnect.RemoveListener(OnConnect);
        _room.onJoin.RemoveListener(OnJoin);
        _room.onGamePhaseChange.RemoveListener(GamePhaseChangeHandler);
    }
}