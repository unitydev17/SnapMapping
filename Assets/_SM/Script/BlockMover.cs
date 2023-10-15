using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class BlockMover : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const float Threshold = 0.1f;

    [SerializeField] private Tile _placeholderTile;
    [SerializeField] private Tile _redTile;
    [SerializeField] private Tile _greenTile;

    [SerializeField] private Tilemap _tilemapPlaceholder;
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private Tilemap _tilemapMoving;

    private Vector3Int _startingPosition;
    private Vector3Int _cellPosition;

    private bool _tap;
    private Camera _cam;

    private readonly List<MyTile> _selectedTiles = new List<MyTile>();
    private bool _hasRedCells;

    private struct MyTile
    {
        public TileBase tile;
        public Vector3Int pos;
    }


    private void Start()
    {
        _cam = Camera.main;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _tap = true;

        var worldPos = eventData.pointerCurrentRaycast.worldPosition;
        _cellPosition = _tilemap.WorldToCell(worldPos);
        _startingPosition = _cellPosition;

        var tile = _tilemap.GetTile(_cellPosition);
        if (!tile.name.Equals(_greenTile.name)) return;


        SelectBlock(tile, _cellPosition);
        ClearBlock(_tilemap);
        UpdateBlockPosition(Vector3Int.zero);
    }


    private void ClearBlock(Tilemap tilemap)
    {
        for (var i = 0; i < _selectedTiles.Count; i++)
        {
            var myTile = _selectedTiles.ElementAt(i);
            tilemap.SetTile(myTile.pos, null);
        }
    }

    private void SelectBlock(TileBase tile, Vector3Int cellPoint)
    {
        _selectedTiles.Clear();

        // Find up rectangular bounds

        var up = GetBound(tile, cellPoint, Vector3Int.up);
        var down = GetBound(tile, cellPoint, Vector3Int.down);
        var left = GetBound(tile, cellPoint, Vector3Int.left);
        var right = GetBound(tile, cellPoint, Vector3Int.right);

        for (var i = down.y; i <= up.y; i++)
        for (var j = left.x; j <= right.x; j++)
        {
            var pos = new Vector3Int(j, i);

            var myTile = new MyTile
            {
                pos = pos,
                tile = _tilemap.GetTile(pos)
            };
            _selectedTiles.Add(myTile);
        }
    }

    private Vector3Int GetBound(TileBase tile, Vector3Int cellPoint, Vector3Int dir)
    {
        var pos = cellPoint;
        Vector3Int prevPos;
        TileBase nextTile;

        do
        {
            prevPos = pos;
            pos += dir;
            nextTile = _tilemap.GetTile(pos);
        } while (nextTile != null);

        return prevPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_tap) return;

        var worldPos = _cam.ScreenToWorldPoint(eventData.position);
        var dragCellPos = _tilemap.WorldToCell(worldPos);

        var deltaPos = dragCellPos - _cellPosition;

        if (deltaPos.sqrMagnitude < Threshold) return;
        _cellPosition = dragCellPos;

        UpdateBlockPosition(deltaPos);
    }

    private void UpdateBlockPosition(Vector3Int deltaPos)
    {
        // 1.Clear tiles

        ClearFigure(_tilemapMoving);

        // 2.Redraw tiles

        _hasRedCells = false;

        for (var i = 0; i < _selectedTiles.Count; i++)
        {
            var myTile = _selectedTiles.ElementAt(i);

            var nextPos = myTile.pos + deltaPos;
            myTile.pos = nextPos;
            _selectedTiles[i] = myTile;


            var targetTile1 = _tilemap.GetTile(nextPos);
            if (targetTile1 != null)
            {
                SetRedCell(ref myTile);
            }
            else
            {
                var targetTile0 = _tilemapPlaceholder.GetTile(nextPos);
                var greenCondition = targetTile0 != null && targetTile0.name.Equals(_placeholderTile.name);


                if (greenCondition)
                {
                    myTile.tile = _greenTile;
                }
                else
                {
                    SetRedCell(ref myTile);
                }
            }

            _tilemapMoving.SetTile(nextPos, myTile.tile);
        }

        void SetRedCell(ref MyTile tile)
        {
            tile.tile = _redTile;
            _hasRedCells = true;
        }
    }

    private void StopMove(Vector3Int deltaPos, Tilemap tilemap)
    {
        for (var i = 0; i < _selectedTiles.Count; i++)
        {
            var myTile = _selectedTiles.ElementAt(i);

            var pos = myTile.pos;
            var nextPos = pos + deltaPos;
            myTile.pos = nextPos;
            _selectedTiles[i] = myTile;

            myTile.tile = _greenTile;
            tilemap.SetTile(nextPos, myTile.tile);
        }
    }

    private void ClearFigure(Tilemap tilemap)
    {
        for (var i = 0; i < _selectedTiles.Count; i++)
        {
            var myTile = _selectedTiles.ElementAt(i);
            tilemap.SetTile(myTile.pos, null);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _tap = false;

        ClearBlock(_tilemapMoving);

        if (_hasRedCells)
        {
            // in case of having red cells place figure in to the starting place
            var delta = _startingPosition - _cellPosition;
            StopMove(delta, _tilemap);
        }
        else
        {
            // if all cells are green then place the figure

            StopMove(Vector3Int.zero, _tilemap);
        }
    }
}