using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text narratorText;
    public Image referenceImage;
    public GameObject selectionGridPanel;

    [Header("Game Configuration")]
    public Sprite[] allItemSprites;
    public ImageIdentity[] optionImages;

    [Range(1, 5)]
    public int maxTargetCount = 3;

    [Header("Narration Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip successClip;
    public AudioClip failureClip;
    public AudioClip partialSuccessClip;

    // Internal state
    private string targetItemName;
    private int totalTargetCount;
    private int foundTargetCount;

    void Start()
    {
        if (allItemSprites.Length < 1 || optionImages.Length < 1)
        {
            Debug.LogError("Setup Error: Missing sprites or option images in GameManager!");
            return;
        }

        foreach (ImageIdentity option in optionImages)
        {
            Button button = option.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => CheckSelection(option));
            }
        }

        if (selectionGridPanel != null)
        {
            selectionGridPanel.SetActive(false);
        }

        SetupRound();
    }

    void SetupRound()
    {
        SetOptionsInteractable(false);
        foundTargetCount = 0;
        totalTargetCount = 0;

        if (selectionGridPanel != null)
        {
            selectionGridPanel.SetActive(false);
        }

        // 1. Determine the Target Item and make it visible
        Sprite targetSprite = allItemSprites[Random.Range(0, allItemSprites.Length)];
        referenceImage.sprite = targetSprite;
        referenceImage.gameObject.SetActive(true);
        targetItemName = targetSprite.name;

        // 2. Determine the required number of items for this round
        int requiredTargetCount = Random.Range(1, maxTargetCount + 1);
        totalTargetCount = requiredTargetCount;

        // 3. Prepare the Selection Pool

        // --- FIX IMPLEMENTATION START ---
        // Create a list of all sprites *excluding* the current target for decoys.
        List<Sprite> decoySprites = new List<Sprite>();
        foreach (Sprite s in allItemSprites)
        {
            if (s.name != targetItemName)
            {
                decoySprites.Add(s);
            }
        }

        if (decoySprites.Count == 0 && allItemSprites.Length > 1)
        {
            // This error check should only happen if all sprite names are identical, 
            // which is a configuration error if maxTargetCount > 1.
            Debug.LogError("Setup Error: Need at least one unique sprite for decoys.");
            return;
        }

        List<Sprite> selectionPool = new List<Sprite>();

        // Add the required number of correct answers (Targets)
        for (int i = 0; i < requiredTargetCount; i++)
        {
            selectionPool.Add(targetSprite);
        }

        // Fill the rest with random decoys (guaranteed not to be the target sprite)
        int remainingSlots = optionImages.Length - requiredTargetCount;
        for (int i = 0; i < remainingSlots; i++)
        {
            // Draw from the filtered decoySprites list
            selectionPool.Add(decoySprites[Random.Range(0, decoySprites.Count)]);
        }
        // --- FIX IMPLEMENTATION END ---

        // 4. Shuffle the Pool
        Shuffle(selectionPool);

        // 5. Display the Options
        for (int i = 0; i < optionImages.Length; i++)
        {
            optionImages[i].SetContent(selectionPool[i], selectionPool[i].name);
            optionImages[i].gameObject.SetActive(true);
        }

        // 6. Start Narration
        string instructionText = $"Find all of the {targetItemName}!";
        PlayNarration(instructionText, introClip);

        // 7. Start the coroutine
        StartCoroutine(HideReferenceImageAndEnableOptions(introClip.length));
    }

    public void CheckSelection(ImageIdentity clickedObject)
    {
        if (!clickedObject.GetComponent<Button>().interactable)
            return;

        StopAllCoroutines();

        if (clickedObject.itemName == targetItemName)
        {
            foundTargetCount++;
            clickedObject.gameObject.SetActive(false);
            SetOptionsInteractable(false);

            if (foundTargetCount == totalTargetCount)
            {
                referenceImage.gameObject.SetActive(false);
                PlayNarration("You found them all! Excellent job!", successClip);
                StartCoroutine(NextRoundAfterDelay(successClip.length));
            }
            else
            {
                PlayNarration($"Found one! Only {totalTargetCount - foundTargetCount} more to go!", partialSuccessClip);
                StartCoroutine(ReEnableOptionsAfterDelay(partialSuccessClip.length));
            }
        }
        else
        {
            SetOptionsInteractable(false);
            PlayNarration("Oops, that's not the right one. Try again!", failureClip);
            StartCoroutine(ReEnableOptionsAfterDelay(failureClip.length));
        }
    }

    IEnumerator HideReferenceImageAndEnableOptions(float delayTime)
    {
        yield return new WaitForSeconds(delayTime + 0.5f);
        referenceImage.gameObject.SetActive(false);

        if (selectionGridPanel != null)
        {
            selectionGridPanel.SetActive(true);
        }
        SetOptionsInteractable(true);
    }

    IEnumerator NextRoundAfterDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime + 0.5f);
        SetupRound();
    }

    IEnumerator ReEnableOptionsAfterDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime + 0.2f);
        SetOptionsInteractable(true);
    }

    void SetOptionsInteractable(bool isInteractable)
    {
        foreach (ImageIdentity option in optionImages)
        {
            Button button = option.GetComponent<Button>();
            if (button != null && option.gameObject.activeSelf)
            {
                button.interactable = isInteractable;
            }
        }
    }

    private void PlayNarration(string text, AudioClip clip)
    {
        narratorText.text = text;
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}