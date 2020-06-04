using Colyseus;

public class GameManager : ColyseusManager<GameManager>
{
    RoomController _roomController = new RoomController();
    public RoomController RoomController => _roomController;

    public override void InitializeClient()
    {
        base.InitializeClient();
        //Pass the newly created Client reference to our RoomController
        _roomController.SetClient(client);
    }
}