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
    // All possible items in the game
    public Sprite[] allItemSprites;
    public ImageIdentity[] optionImages;

    [Range(1, 5)] // Ensures the designer sets a reasonable max count in the Inspector
    public int maxTargetCount = 3; // The maximum number of the target item that can appear

    [Header("Narration Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip successClip;
    public AudioClip failureClip;
    public AudioClip partialSuccessClip; // New Clip: For when one correct item is selected, but more remain

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
                // Assign a listener that calls CheckSelection with this specific ImageIdentity
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
        foundTargetCount = 0; // Reset count for the new round
        totalTargetCount = 0; // Reset total count

        if (selectionGridPanel != null)
        {
            selectionGridPanel.SetActive(false);
        }

        // 1. Determine the Target Item and make it visible
        Sprite targetSprite = allItemSprites[Random.Range(0, allItemSprites.Length)];
        referenceImage.sprite = targetSprite;
        referenceImage.gameObject.SetActive(true);
        targetItemName = targetSprite.name;

        // 2. Determine the required number of items for this round (between 1 and maxTargetCount)
        int requiredTargetCount = Random.Range(1, maxTargetCount + 1);
        totalTargetCount = requiredTargetCount;

        // 3. Prepare the Selection Pool
        List<Sprite> selectionPool = new List<Sprite>();

        // Add the required number of correct answers (Targets)
        for (int i = 0; i < requiredTargetCount; i++)
        {
            selectionPool.Add(targetSprite);
        }

        // Fill the rest with random decoys
        int remainingSlots = optionImages.Length - requiredTargetCount;
        for (int i = 0; i < remainingSlots; i++)
        {
            selectionPool.Add(allItemSprites[Random.Range(0, allItemSprites.Length)]);
        }

        // 4. Shuffle the Pool
        Shuffle(selectionPool);

        // 5. Display the Options
        for (int i = 0; i < optionImages.Length; i++)
        {
            optionImages[i].SetContent(selectionPool[i], selectionPool[i].name);
            // Ensure all option images are visible (they will be hidden by the parent panel)
            optionImages[i].gameObject.SetActive(true);
        }

        // 6. Start Narration
        string instructionText = $"Find all {totalTargetCount} of the {targetItemName}!";
        PlayNarration(instructionText, introClip);

        // 7. Start the coroutine to hide the reference image and SHOW the options grid
        StartCoroutine(HideReferenceImageAndEnableOptions(introClip.length));
    }

    // UPDATED CheckSelection method signature to take the ImageIdentity object
    public void CheckSelection(ImageIdentity clickedObject)
    {
        // Check if the game is currently allowing interaction
        if (!clickedObject.GetComponent<Button>().interactable)
            return;

        // Stop the HideReferenceImage coroutine immediately
        StopAllCoroutines();

        if (clickedObject.itemName == targetItemName)
        {
            // --- CORRECT SELECTION ---
            foundTargetCount++;

            // 1. Visually remove the found item
            clickedObject.gameObject.SetActive(false);

            // 2. Disable all buttons temporarily for feedback
            SetOptionsInteractable(false);

            if (foundTargetCount == totalTargetCount)
            {
                // --- FINAL CORRECT SELECTION (Round Complete) ---
                referenceImage.gameObject.SetActive(false);
                PlayNarration("You found them all! Excellent job!", successClip);
                StartCoroutine(NextRoundAfterDelay(successClip.length));
            }
            else
            {
                // --- PARTIAL SUCCESS ---
                PlayNarration($"Found one! Only {totalTargetCount - foundTargetCount} more to go!", partialSuccessClip);
                // Re-enable options after the partial success feedback audio plays
                StartCoroutine(ReEnableOptionsAfterDelay(partialSuccessClip.length));
            }
        }
        else
        {
            // --- INCORRECT SELECTION ---
            SetOptionsInteractable(false); // Disable all buttons
            PlayNarration("Oops, that's not the right one. Try again!", failureClip);

            // Re-enable options after the failure audio
            StartCoroutine(ReEnableOptionsAfterDelay(failureClip.length));
        }
    }

    // Coroutine: Hides the reference image and ACTIVATES the selection grid
    IEnumerator HideReferenceImageAndEnableOptions(float delayTime)
    {
        yield return new WaitForSeconds(delayTime + 0.5f);

        // 1. Hide the ReferenceImage
        referenceImage.gameObject.SetActive(false);

        // 2. ACTIVATE the entire selection grid GameObject
        if (selectionGridPanel != null)
        {
            selectionGridPanel.SetActive(true);
        }

        // 3. ENABLE the selection options so the player can click
        SetOptionsInteractable(true);
    }

    // Coroutine: Delays and then starts the next round
    IEnumerator NextRoundAfterDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime + 0.5f);
        SetupRound();
    }

    // Coroutine: Delays and then re-enables options after a failed attempt or partial success
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
            if (button != null && option.gameObject.activeSelf) // Only affect active options
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

    // Simple List Shuffler (Fisher-Yates)
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