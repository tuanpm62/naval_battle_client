using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public enum ShipType
{
    Destroyer = 1,
    Cruiser,
    Submarine,
    BattleShip,
    AircraftCarrier,
}

public enum Marker
{
    Target = 10,
    Hit,
    Miss
}

public enum MapMode
{
    Disabled,
    Place,
    Attack
}

public class MapView : MonoBehaviour
{
    public Tilemap fleetLayer;

    public Tilemap markerLayer;

    public Tilemap cursorLayer;

    public Tile[] cursorTiles;

    private Tile _cursorTile;

    private const int Size = 10;

    private Grid _grid;

    private Camera _mainCamera;

    private Vector3Int _minCoordinate;

    private Vector3Int _maxCoordinate;

    private MapMode _mode;

    private Vector2 _mSelect;

    private void Start()
    {
        _mainCamera = Camera.main;
        _grid = GetComponent<Grid>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _mSelect = context.ReadValue<Vector2>();
    }

    public void OnSelect(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:
                Select();
                break;
        }
    }

    private void Select()
    {
        var pos = _mainCamera.ScreenToWorldPoint(new Vector3(_mSelect.x,
            _mSelect.y, _mainCamera.transform.position.z * -1));
        var coordinate = _grid.WorldToCell(pos);

        coordinate.Clamp(_minCoordinate, _maxCoordinate);

        switch (_mode)
        {
            case MapMode.Place:
                GameSceneController.Instance.PlaceShip(coordinate);
                break;
            case MapMode.Attack:
                GameSceneController.Instance.TakeTurn(coordinate - new Vector3Int(0, Size, 0));
                break;
            case MapMode.Disabled:
                break;
            default:
                Debug.LogWarning("Argument Out Of Range Exception | " + _mode);
                break;
        }
    }

    private void Update()
    {
        if (_mode == MapMode.Disabled) return;
        var pos = _mainCamera.ScreenToWorldPoint(new Vector3(Mouse.current.position.ReadValue().x,
            Mouse.current.position.ReadValue().y, _mainCamera.transform.position.z * -1));
        var coordinate = _grid.WorldToCell(pos);

        coordinate.Clamp(_minCoordinate, _maxCoordinate);

        cursorLayer.ClearAllTiles();
        cursorLayer.SetTile(coordinate, _cursorTile);
    }

    public void SetDisabled()
    {
        _mode = MapMode.Disabled;
        cursorLayer.ClearAllTiles();
    }

    public void SetPlacementMode()
    {
        _mode = MapMode.Place;
        _minCoordinate = new Vector3Int(0, 0, 0);
        _maxCoordinate = new Vector3Int(Size - 1, Size - 1, 0);
    }

    public void SetAttackMode()
    {
        _mode = MapMode.Attack;
        _cursorTile = cursorTiles[Marker.Target.GetHashCode()];
        _minCoordinate = new Vector3Int(0, Size, 0);
        _maxCoordinate = new Vector3Int(Size - 1, Size + Size - 1, 0);
    }

    public void SetShipCursor(ShipType shipType, bool horizontal)
    {
        var index = (shipType.GetHashCode() - 1) * 2 + (horizontal ? 1 : 0);
        _cursorTile = cursorTiles[index];
    }

    public void SetShip(ShipType shipType, Vector3Int coordinate, bool horizontal)
    {
        var index = (shipType.GetHashCode() - 1) * 2 + (horizontal ? 1 : 0);
        var tile = cursorTiles[index];
        fleetLayer.SetTile(coordinate, tile);
    }

    public void SetMarker(int index, Marker marker, bool radar)
    {
        var coordinate = new Vector3Int(index % Size, Mathf.FloorToInt(index * 1.0f / Size), 0);
        Debug.Log("marker " + marker + " " + index);
        SetMarker(coordinate, marker, radar);
    }

    private void SetMarker(Vector3Int coordinate, Marker marker, bool radar)
    {
        if (radar)
        {
            coordinate += new Vector3Int(0, Size, 0);
        }

        markerLayer.SetTile(coordinate, cursorTiles[marker.GetHashCode()]);
    }
}