using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] GameObject mainAI;

    bool isPlacing;
    bool mouseDown;
    public bool isUserMode;
    public bool UserProvidedFeedback { get; set; }
    public bool IsAnalysisMode { get; set; }
    public float DecisionOutcome { get; set; }
    public bool AllowPassiveLearning { get; set; }
    RaycastHit hitInfo = new();
    [SerializeField] GameObject newBlock, blockPrefab, placeManager, blockHighlight;

    [SerializeField] Transform allBlocks;

    [SerializeField] GameObject panel;
    [SerializeField] GameObject gameStuff;
    [SerializeField] TextMeshProUGUI scoreText;

    [SerializeField] GameObject winAndLoseStuff;
    [SerializeField] TextMeshProUGUI outcomeText;

    [SerializeField] TextMeshProUGUI locationText, weightText;

    /// <summary>
    /// Store the current score. Player score (shutting down the AI) is first, followed by AI escaping.
    /// </summary>
    Vector2 currentScore = Vector2.zero;

    private void Awake()
    {
        Instance = this;

        placeManager.SetActive(false);
        blockHighlight.SetActive(false);
    }

    public void RunSequence()
    {
        if (isPlacing)
        {
            return;
        }
        StartCoroutine(MainSequence());
    }

    private void Update()
    {
        mouseDown = Input.GetMouseButtonDown(0);
    }

    IEnumerator MainSequence()
    {
        yield return new WaitForSeconds(0.25f);

        yield return StartCoroutine(PlaceBlock());
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(MainAI.Instance.AISequence());
    }

    IEnumerator GANSequence()
    {
        yield return null;
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
        else if (hitX > 8)
        {
            hitInfoX = 8;
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
        else if (hitZ > 8)
        {
            hitInfoZ = 8;
        }
        else
        {
            hitInfoZ = Mathf.Round(hitZ);
        }
        try
        {
            i.transform.position = new Vector3(hitInfoX, 1, hitInfoZ);
        }
        catch
        {
            return;
        }
    }

    IEnumerator PlaceBlock()
    {
        isPlacing = true;
        placeManager.SetActive(true);
        blockHighlight.SetActive(true);
        Ray ray;

        Destroy(newBlock);
        newBlock = Instantiate(blockPrefab, allBlocks);

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray.origin, ray.direction, out hitInfo))
        {
            PlaceWithin(newBlock, hitInfo.point.x, hitInfo.point.z);
            placeManager.transform.position = newBlock.transform.position;
            blockHighlight.transform.position = newBlock.transform.position;
        }

        while (true)
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out hitInfo))
            {
                PlaceWithin(newBlock, hitInfo.point.x, hitInfo.point.z);
                placeManager.transform.position = newBlock.transform.position;
                blockHighlight.transform.position = newBlock.transform.position;
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
        blockHighlight.SetActive(false);

        // update the grid of the MainAI to contain a block
        int newBlockX = (int)newBlock.transform.position.x;
        int newBlockZ = (int)newBlock.transform.position.z;
        MainAI.Instance.AddBlock(newBlockX, newBlockZ);
        newBlock = null;
    }

    public void GameEnd(bool didAIWin)
    {
        StopAllCoroutines();

        Destroy(newBlock);

        ActiveManager.Instance.GameEnd();

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
        ActiveManager.Instance.PlayGame();

        mainAI.transform.position = new Vector3(4, 1, 4);
        MainAI.Instance.ResetGrid();

        for (int i = 0; i < allBlocks.transform.childCount; i++)
        {
            Destroy(allBlocks.GetChild(i).gameObject);
        }

        UserProvidedFeedback = false;
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

    /// <summary>
    /// This function is called by blocks when they are clicked on in analysis mode.
    /// </summary>
    public void Analyze(int posX, int posZ)
    {
        Vector2 blockPos = new(posX, posZ);
        blockHighlight.SetActive(true);
        locationText.text = "Block Location: (" + posX + ", " + posZ + ")";
        weightText.text = "Block Weight: " + MainAI.Instance.GetBlockWeight(blockPos).ToString("F3");

        blockHighlight.transform.position = new Vector3(posX, 1, posZ);
    }
}
