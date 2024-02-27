using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    Coroutine mainSequence;
    [SerializeField] GameObject mainAI;

    bool isPlacing;
    bool mouseDown;
    RaycastHit hitInfo = new();
    GameObject newBlock;
    [SerializeField] GameObject blockPrefab;
    [SerializeField] GameObject placeManager;

    [SerializeField] Transform allBlocks;

    [SerializeField] GameObject panel;
    [SerializeField] GameObject gameStuff;
    [SerializeField] TextMeshProUGUI scoreText;

    [SerializeField] GameObject winAndLoseStuff;
    [SerializeField] TextMeshProUGUI outcomeText;

    /// <summary>
    /// Store the current score. Player score (shutting down the AI) is first, followed by AI escaping.
    /// </summary>
    Vector2 currentScore = Vector2.zero;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        placeManager.SetActive(false);
    }

    public void StartGame()
    {
        if (mainSequence == null)
        {
            mainSequence = StartCoroutine(MainSequence());
        }
        else
        {
            StartCoroutine(MainSequence());
        }
    }

    private void Update()
    {
        mouseDown = Input.GetMouseButtonDown(0);
    }

    IEnumerator MainSequence()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            yield return StartCoroutine(PlaceBlock());
            yield return new WaitForSeconds(0.5f);

            MainAI.Instance.AISequence();
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

    public void GameEnd(bool didAIWin)
    {
        StopCoroutine(mainSequence);

        panel.SetActive(true);
        winAndLoseStuff.SetActive(true);

        Destroy(newBlock);
        if (didAIWin)
        {
            outcomeText.text = "You Lost!";

            currentScore += Vector2.up;
        }
        else
        {
            outcomeText.text = "You Win!";

            currentScore += Vector2.right;
        }
        scoreText.text = "Score: " + currentScore.x + "-" + currentScore.y;
    }

    public void ResetGame()
    {
        panel.SetActive(false);
        winAndLoseStuff.SetActive(false);

        mainAI.transform.position = new Vector3(4, 1, 4);
        MainAI.Instance.ResetGrid();

        for (int i = 0; i < allBlocks.transform.childCount; i++)
        {
            Destroy(allBlocks.GetChild(i).gameObject);
        }

        StartCoroutine(MainSequence());
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
                    newBlock.GetComponent<BlockedSquare>().ErrorFlash();
                }
            }

            yield return new WaitForEndOfFrame();
        }

        newBlock.GetComponent<BoxCollider>().enabled = true;
        newBlock.transform.localScale = Vector3.one * 0.75f;
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
