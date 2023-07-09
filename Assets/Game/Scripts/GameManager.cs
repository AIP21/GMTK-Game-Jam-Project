using System.Collections;
using Chronos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    #region References
    [Header("References")]
    public SoccerPlayerSpawner SoccerPlayerSpawner;
    public SoccerBallController Player;
    #endregion

    #region Score
    [Header("Score")]
    public int Score = 0;
    public int Difficulty = 1;

    public int ScoreToWin = 5;


    #endregion

    #region UI
    [Header("UI")]
    public UIScreen WinScreen;
    public UIScreen LoseScreen;
    public UIScreen MainMenuScreen;
    public UIScreen InfoScreen;
    public TextMeshProUGUI WinScoreText;
    public TextMeshProUGUI LoseScoreText;

    [Space(10)]
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI DifficultyText;

    [Space(10)]
    public Slider JumpCooldownSlider;
    #endregion

    #region Effects
    [Header("Effects")]
    public ParticleSystem GoalFX;

    public Timekeeper TimeScale;
    private GlobalClock clock;
    #endregion

    #region Sound
    [Header("Sound")]
    public AudioSource AudioSource;

    public AudioClip GoalSound;
    public AudioClip WinSound;
    public AudioClip LoseSound;
    #endregion

    #region Other
    [Header("Other")]
    public Vector3 SpawnPoint;
    #endregion

    public void Awake()
    {
        // Assign the singleton instance
        Instance = this;

        clock = TimeScale.Clock("Root");
    }

    // Start is called before the first frame update
    public void Play()
    {
        MainMenuScreen.Hide();
        LoseScreen.Hide();
        WinScreen.Hide();
        InfoScreen.Hide();
        NewGame();
    }

    public void Info()
    {
        InfoScreen.Show();
    }

    public void Quit()
    {
        Application.Quit();
    }

    // Update is called once per frame
    public void Update()
    {
        JumpCooldownSlider.value = (float)Player.JumpCooldown / (float)Player.MaxJumpCooldown;

        // Change slider color
        JumpCooldownSlider.fillRect.GetComponent<Image>().color = Color.Lerp(Color.red, Color.green, JumpCooldownSlider.value);
    }

    public void Scored()
    {
        Score++;
        ScoreText.text = "Score: " + Score.ToString();

        Difficulty += 1;
        DifficultyText.text = "Difficulty: " + Score.ToString();

        StartCoroutine(ScoreEffect());
    }

    private IEnumerator ScoreEffect()
    {
        // Player.rb.AddExplosionForce(10, Player.transform.position + Player.transform.forward * 4 - Vector3.up * 2, 10f, 4, ForceMode.Impulse);

        // Slow down time
        clock.LerpTimeScale(0.3f, 1, true);

        // Slow down the player
        Player.rb.velocity = Player.rb.velocity * 0.5f;
        Player.isStopped = true;
        Player.Trail.emitting = true;

        // Play a sound and fx
        AudioSource.PlayOneShot(GoalSound);
        GoalFX.gameObject.transform.position = Player.transform.position;
        GoalFX.Play();

        // Wait for a bit
        yield return new WaitForSecondsRealtime(2f);

        clock.LerpTimeScale(1, 0.5f, true);

        GoalFX.Stop();

        // Reset soccer players
        SoccerPlayerSpawner.Reset(Difficulty);

        // Check if the player has won
        if (Score >= ScoreToWin)
            WinGame();

        // Reset player pos
        Player.rb.velocity = Vector3.zero;
        Player.rb.angularVelocity = Vector3.zero;
        Player.Begin();

        Player.transform.position = SpawnPoint;
        Player.Trail.emitting = false;

        yield return null;
    }

    public void WinGame()
    {
        // Play a sound
        AudioSource.PlayOneShot(WinSound);

        // Set win text
        WinScoreText.text = "Score: " + Score.ToString();

        // Stop the player
        Player.Stop();

        // Show the win screen
        WinScreen.Show();
    }

    public void Lose()
    {
        // Play a sound
        AudioSource.PlayOneShot(LoseSound);

        // Set win text
        LoseScoreText.text = "Score: " + Score.ToString();

        // Stop the player
        Player.Stop();

        // Show the win screen
        LoseScreen.Show();
    }

    public void NewGame()
    {
        WinScreen.Hide();

        Score = 0;
        Difficulty = 1;
        ScoreText.text = "Score: " + Score.ToString();
        DifficultyText.text = "Difficulty: " + Score.ToString();

        SoccerPlayerSpawner.Reset(Difficulty);
        Player.transform.position = SpawnPoint;
        Player.Begin();
    }
}