
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RadioKnobController : MonoBehaviour
{
    public enum Position
    {
        Play,
        Info,
        Exit
    }

    [Header("Componentes")]
    public Animator animator;
    public Image knobImage;
    public Sprite neutralSprite;

    [Header("Animaciones desde Neutral")]
    public string neutralToPlay = "neutral_play";
    public string neutralToInfo = "neutral_info";
    public string neutralToExit = "neutral_exit";

    [Header("Animaciones entre opciones")]
    public string playToInfo = "play_info";
    public string playToExit = "play_exit";

    public string infoToPlay = "info_play";
    public string infoToExit = "info_exit";

    public string exitToPlay = "exit_play";
    public string exitToInfo = "exit_info";

    private Position currentPosition;
    private Position requestedPosition;
    private bool hasSelected = false;
    private Coroutine movement;

    void Start()
    {
        animator.enabled = false;
        knobImage.sprite = neutralSprite;
    }

    public void HoverPlay()
    {
        MoveTo(Position.Play);
    }

    public void HoverInfo()
    {
        MoveTo(Position.Info);
    }

    public void HoverExit()
    {
        MoveTo(Position.Exit);
    }

    private void MoveTo(Position target)
    {
        requestedPosition = target;

        if (movement == null)
        {
            animator.enabled = true;
            movement = StartCoroutine(AnimateMovement());
        }
    }

    private IEnumerator AnimateMovement()
    {
        while (!hasSelected || currentPosition != requestedPosition)
        {
            Position destination = requestedPosition;
            string animationName;

            if (!hasSelected)
            {
                if (destination == Position.Play)
                    animationName = neutralToPlay;
                else if (destination == Position.Info)
                    animationName = neutralToInfo;
                else
                    animationName = neutralToExit;
            }
            else
            {
                animationName = GetAnimation(currentPosition, destination);
            }

            if (hasSelected && currentPosition == destination)
                break;

            animator.Play(animationName, 0, 0f);

            yield return null;

            float duration = animator.GetCurrentAnimatorStateInfo(0).length;

            yield return new WaitForSecondsRealtime(duration);

            animator.enabled = false;

            currentPosition = destination;
            hasSelected = true;
        }

        movement = null;
    }

    private string GetAnimation(Position from, Position to)
    {
        if (from == Position.Play)
        {
            if (to == Position.Info) return playToInfo;
            if (to == Position.Exit) return playToExit;
        }

        if (from == Position.Info)
        {
            if (to == Position.Play) return infoToPlay;
            if (to == Position.Exit) return infoToExit;
        }

        if (from == Position.Exit)
        {
            if (to == Position.Play) return exitToPlay;
            if (to == Position.Info) return exitToInfo;
        }

        return "";
    }
}
