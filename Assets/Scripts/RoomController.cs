using Colyseus;

public class RoomController
{
    ColyseusClient _client;
    public ColyseusClient Client => _client;

    public void SetClient(ColyseusClient client)
    {
        _client = client;
        ColyseusRoom.Instance.OnOpenHandler();
    }
}