using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemySpace {
    /// <summary>
    /// Menedżer kolejkowania żądań ścieżek A*.
    /// </summary>
    public class PathFindingManager : MonoBehaviour {
        private struct PathRequest {
            public Vector3 start;
            public Vector3 target;
            public Action<List<Vector3>> callback;
        }

        private Queue<PathRequest> requests = new Queue<PathRequest>();
        private bool isProcessingPath = false;

        public static PathFindingManager Instance { get; private set; }

        [Header("Map References")]
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private LayerMask wallMask;

        // Pooling struktur
        private MinHeap openSet = new MinHeap();
        private Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        private Dictionary<Vector3Int, float> gScore = new Dictionary<Vector3Int, float>();
        private Dictionary<Vector3Int, float> fScore = new Dictionary<Vector3Int, float>();
        private HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        private void Awake() {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (tilemapCollider == null)
                tilemapCollider = FindObjectOfType<TilemapCollider2D>();
            if (tilemap == null && tilemapCollider != null)
                tilemap = tilemapCollider.GetComponent<Tilemap>();
        }

        public void RequestPath(Vector3 from, Vector3 to, Action<List<Vector3>> callback) {
            requests.Enqueue(new PathRequest { start = from, target = to, callback = callback });
            TryProcessNext();
        }

        private void TryProcessNext() {
            if (!isProcessingPath && requests.Count > 0) {
                PathRequest req = requests.Dequeue();
                StartCoroutine(ProcessPath(req));
            }
        }

        private IEnumerator ProcessPath(PathRequest req) {
            isProcessingPath = true;
            List<Vector3> path = FindPathAStar(req.start, req.target);
            req.callback(path);
            yield return new WaitForSeconds(0.05f);
            isProcessingPath = false;
            TryProcessNext();
        }

        private List<Vector3> FindPathAStar(Vector3 startWorld, Vector3 targetWorld) {
            Vector3Int startCell = tilemap.WorldToCell(startWorld);
            Vector3Int targetCell = tilemap.WorldToCell(targetWorld);

            // Reset pooled structures
            openSet.Clear();
            cameFrom.Clear();
            gScore.Clear();
            fScore.Clear();
            visited.Clear();

            // Early exit
            if (IsCellBlocked(startCell) || IsCellBlocked(targetCell))
                return null;

            gScore[startCell] = 0f;
            fScore[startCell] = Heuristic(startCell, targetCell);
            openSet.Add(startCell, fScore[startCell]);

            while (openSet.Count > 0) {
                Vector3Int current = openSet.Pop();
                if (current == targetCell)
                    return ReconstructPath(cameFrom, current);

                visited.Add(current);

                foreach (Vector3Int neighbor in GetNeighbors(current)) {
                    if (visited.Contains(neighbor) || IsCellBlocked(neighbor))
                        continue;

                    float tentativeG = gScore[current] + Vector3Int.Distance(current, neighbor);
                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor]) {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, targetCell);

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor, fScore[neighbor]);
                        else
                            openSet.UpdatePriority(neighbor, fScore[neighbor]);
                    }
                }
            }

            return null;
        }

        private bool IsCellBlocked(Vector3Int cell) {
            if (!tilemap.HasTile(cell)) return false;
            return true;
        }

        private float Heuristic(Vector3Int a, Vector3Int b) {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell) {
            yield return new Vector3Int(cell.x + 1, cell.y, 0);
            yield return new Vector3Int(cell.x - 1, cell.y, 0);
            yield return new Vector3Int(cell.x, cell.y + 1, 0);
            yield return new Vector3Int(cell.x, cell.y - 1, 0);
        }

        private List<Vector3> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current) {
            var totalPath = new List<Vector3Int> { current };
            while (cameFrom.ContainsKey(current)) {
                current = cameFrom[current];
                totalPath.Add(current);
            }
            totalPath.Reverse();

            var worldPath = new List<Vector3>();
            foreach (var cell in totalPath)
                worldPath.Add(tilemap.GetCellCenterWorld(cell));

            return worldPath;
        }

        private class MinHeap {
            private List<(Vector3Int cell, float priority)> heap = new List<(Vector3Int, float)>();
            private Dictionary<Vector3Int, int> indices = new Dictionary<Vector3Int, int>();

            public int Count => heap.Count;

            public void Clear() {
                heap.Clear();
                indices.Clear();
            }

            public void Add(Vector3Int cell, float priority) {
                heap.Add((cell, priority));
                int i = heap.Count - 1;
                indices[cell] = i;
                BubbleUp(i);
            }

            public bool Contains(Vector3Int cell) => indices.ContainsKey(cell);

            public void UpdatePriority(Vector3Int cell, float newPriority) {
                if (!indices.TryGetValue(cell, out int i)) return;
                float old = heap[i].priority;
                heap[i] = (cell, newPriority);
                if (newPriority < old) BubbleUp(i);
                else BubbleDown(i);
            }

            public Vector3Int Pop() {
                var root = heap[0].cell;
                Swap(0, heap.Count - 1);
                heap.RemoveAt(heap.Count - 1);
                indices.Remove(root);
                BubbleDown(0);
                return root;
            }

            private void BubbleUp(int i) {
                while (i > 0) {
                    int parent = (i - 1) / 2;
                    if (heap[i].priority < heap[parent].priority) {
                        Swap(i, parent);
                        i = parent;
                    } else break;
                }
            }

            private void BubbleDown(int i) {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;

                if (left < heap.Count && heap[left].priority < heap[smallest].priority)
                    smallest = left;
                if (right < heap.Count && heap[right].priority < heap[smallest].priority)
                    smallest = right;

                if (smallest != i) {
                    Swap(i, smallest);
                    BubbleDown(smallest);
                }
            }

            private void Swap(int i, int j) {
                var tmp = heap[i];
                heap[i] = heap[j];
                heap[j] = tmp;
                indices[heap[i].cell] = i;
                indices[heap[j].cell] = j;
            }
        }
    }
}