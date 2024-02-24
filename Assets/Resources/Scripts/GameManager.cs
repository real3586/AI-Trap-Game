using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    bool isPlacing;
    bool mouseDown;
    RaycastHit hitInfo = new();
    GameObject newBlock;
    GameObject blockPrefab;
    GameObject placeManager;

    Transform allBlocks;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(Instance);

        AssignMissing();
    }

    void AssignMissing()
    {
        placeManager = GameObject.Find("PlaceManager");
        blockPrefab = (GameObject)Resources.Load("Prefabs/BlockedSquare", typeof(GameObject));
        allBlocks = GameObject.Find("AllBlocks").transform;

        placeManager.SetActive(false);

        // temporary please delete
        StartCoroutine(MainSequence());
    }

    private void Update()
    {
        mouseDown = Input.GetMouseButtonDown(0);
    }

    IEnumerator MainSequence()
    {
        while (true)
        {
            yield return StartCoroutine(PlaceBlock());
            yield return new WaitForSeconds(0.5f);

            MainAI.Instance.MoveAI(Random.Range(0, 4));

            yield return null;
        }
    }

    // code from block defense lol
    void PlaceWithin(GameObject i, float hitX, float hitZ)
    {
        // places a block within the boundaries of the map
        float hitInfoX;
        if (hitX < 0)
        {
            hitInfoX = 0;
        }
        else if (hitX > 19)
        {
            hitInfoX = 19;
        }
        else
        {
            hitInfoX = Mathf.Round(hitX);
        }
        // now for z
        float hitInfoZ;
        if (hitZ < 0)
        {
            hitInfoZ = 0;
        }
        else if (hitZ > 14)
        {
            hitInfoZ = 14;
        }
        else
        {
            hitInfoZ = Mathf.Round(hitZ);
        }
        i.transform.position = new Vector3(hitInfoX, 1, hitInfoZ);
    }

    IEnumerator PlaceBlock()
    {
        isPlacing = true;
        placeManager.SetActive(true);
        Ray ray;

        newBlock = Instantiate(blockPrefab, allBlocks);

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray.origin, ray.direction, out hitInfo))
        {
            PlaceWithin(newBlock, hitInfo.point.x, hitInfo.point.z);
            placeManager.transform.position = newBlock.transform.position;
        }

        while (true)
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out hitInfo))
            {
                PlaceWithin(newBlock, hitInfo.point.x, hitInfo.point.z);
                placeManager.transform.position = newBlock.transform.position;
            }

            if (mouseDown)
            {
                if (!placeManager.GetComponent<PlaceManager>().CollidingWithAnything && DidHitFloor() && !MainAI.Instance.IsLerping)
                {
                    break;
                }
                else
                {
                    Debug.Log("cannot place there noob");
                }
            }

            yield return new WaitForEndOfFrame();
        }

        newBlock.GetComponent<BoxCollider>().enabled = true;
        newBlock.tag = "Block";
        isPlacing = false;
        placeManager.SetActive(false);

        // update the grid of the MainAI to contain a block
        MainAI.Instance.AddBlock((int)newBlock.transform.position.x, (int)newBlock.transform.position.z);
    }

    bool DidHitFloor()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray.origin, ray.direction, out hitInfo, 100))
        {
            if (hitInfo.collider.CompareTag("Block") || hitInfo.collider.CompareTag("Player"))
            {
                return false;
            }
        }
        return true;
    }
}
