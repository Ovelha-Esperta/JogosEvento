using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class GameMenuManager : MonoBehaviour, IMenuManager
{
  protected enum GameState
  {
    Idle,
    ShowingSequence,
    WaitingInput,
    Paused,
    Finished,
  }

  [SerializeField]
  protected List<GameObject> _buttons;

  [SerializeField]
  protected TextMeshProUGUI _currentRoundOutput;

  [Header("Flash Colors")]
  [SerializeField]
  private Color _flashColor = Color.yellow;

  [Header("Timing")]
  [SerializeField]
  protected float _initialInterval = 1f;

  [SerializeField, Range(0.5f, 1f)]
  protected float _intervalDecay = 0.9f;

  [SerializeField]
  protected float _minInterval = 0.25f;

  [SerializeField, Range(0.3f, 1f)]
  protected float _popupRatio = 0.8f;

  [SerializeField]
  protected float _delayBeforeSequence = 0.75f;

  [SerializeField]
  protected float _inputFeedbackDuration = 0.2f;

  [Header("Pause")]
  [Tooltip(
      "Ao retomar, mostra a sequência do round de novo (o progresso de input do round é zerado)."
  )]
  [SerializeField]
  protected bool _replaySequenceOnResume = false;

  [Header("Lives")]
  [SerializeField, Min(1)]
  protected int _maxLives = 3;

  [SerializeField]
  protected TextMeshProUGUI _livesOutput;

  [Tooltip(
      "Ao errar com vidas restantes, mostra a sequência do round de novo. Desligado: o jogador tenta o mesmo passo outra vez."
  )]
  [SerializeField]
  protected bool _replaySequenceOnMistake = true;

  [SerializeField]
  protected Color _damageFlashColor = Color.red;

  [SerializeField, Min(0.05f)]
  protected float _damageFlashDuration = 0.4f;

  [Header("Save Prompt")]
  [SerializeField]
  private CanvasGroup _savePanel;

  [SerializeField]
  private TMP_InputField _nameInput;

  [SerializeField]
  private Button _confirmSaveButton;

  [SerializeField]
  private Button _declineSaveButton;

  [SerializeField]
  private PopupTween _savePopup = new();

  [Header("Return To Menu")]
  [SerializeField]
  private MenuManager _menuManager;

  [Tooltip("Segundos sem interação após o fim da partida até voltar ao menu sozinho.")]
  [SerializeField, Min(1f)]
  private float _returnToMenuTimeout = 180f;

  protected ButtonSequence _currentSequence;

  protected GameState _state = GameState.Idle;
  protected int _round;
  protected int _roundsCompleted;
  protected int _inputIndex;
  protected int _lives;

  private GameState _stateBeforePause;
  private bool _isShown;
  private bool _savePromptOpen;
  private Coroutine _showRoutine;
  private Tween _promptTween;
  private Tween _timeoutTween;
  private Tween _damageTween;
  private Color _livesBaseColor;
  private bool _livesBaseColorCaptured;


  private const int MAXNAMELENGTH = 20;
  private readonly List<(Button button, UnityAction action)> _bindings = new();

  public void OnShow()
  {
    _isShown = true;

    LoadGame();
    BindButtons();
    BindSavePrompt();

    if (_state == GameState.Paused)
      ResumeGame();
    else if (_state == GameState.Idle || _state == GameState.Finished)
      StartGame();
  }

  public void OnHide()
  {
    _isShown = false;

    UnLoadGame();
    PauseGame();
    UnbindButtons();
    UnbindSavePrompt();
    ResetLivesVisual();
    HideSavePromptInstant();
  }

  public void ResetGame()
  {
    if (_isShown)
      StartGame();
    else
      StopGame();
  }

  protected virtual void LoadGame() { }

  protected virtual void UnLoadGame() { }

  protected abstract bool IsLastRound(int round);

  protected virtual bool HasProgressToSave(bool won) => true;

  protected virtual void SaveProgress() { }

  protected virtual int GetSequenceLength(int round) => round;

  protected virtual void OnLivesChanged(int lives, int maxLives) { }

  protected virtual float GetInterval(int round) =>
      Mathf.Max(_minInterval, _initialInterval * Mathf.Pow(_intervalDecay, round - 1));

  protected virtual int GenerateNextIndex() => Random.Range(0, _buttons.Count);

  protected virtual List<int> GenerateSequence(int round)
  {
    List<int> previous = _currentSequence.Current;
    List<int> sequence = previous != null ? new List<int>(previous) : new List<int>();

    int length = GetSequenceLength(round);
    while (sequence.Count < length)
      sequence.Add(GenerateNextIndex());

    return sequence;
  }

  protected virtual void OnGameStarted() { }

  protected virtual void OnGamePaused() { }

  protected virtual void OnGameResumed() { }

  protected virtual void OnRoundStarted(int round, float interval) { }

  protected virtual void OnRoundCompleted(int round) { }

  protected virtual void OnGameWon() { }

  protected virtual void OnGameOver(int roundsCompleted)
  {
  }

  protected void StartGame()
  {
    StopGame();

    _currentSequence.Clear();
    _round = 0;
    _roundsCompleted = 0;
    _lives = _maxLives;

    OnGameStarted();
    NotifyLivesChanged();
    NextRound();
  }

  private void HandleMistake()
  {
    _lives--;
    _inputIndex = 0;
    NotifyLivesChanged();
    FlashLives();

    if (_lives <= 0)
    {
      Finish(won: false);
      return;
    }

    if (_replaySequenceOnMistake)
    {
      _showRoutine = StartCoroutine(ShowSequenceRoutine());
    }
  }

  private void StopGame()
  {
    StopShowRoutine();
    ResetButtonVisuals();
    ResetLivesVisual();
    HideSavePromptInstant();
    _state = GameState.Idle;
  }

  private void PauseGame()
  {
    if (_state != GameState.ShowingSequence && _state != GameState.WaitingInput)
      return;

    _stateBeforePause = _state;
    _state = GameState.Paused;

    StopShowRoutine();
    ResetButtonVisuals();

    OnGamePaused();
  }

  private void ResumeGame()
  {
    OnGameResumed();

    if (_replaySequenceOnResume || _stateBeforePause == GameState.ShowingSequence)
    {
      _inputIndex = 0;
      _showRoutine = StartCoroutine(ShowSequenceRoutine());
    }
    else
    {
      _state = GameState.WaitingInput;
    }
  }

  private void NextRound()
  {
    _round++;
    _inputIndex = 0;

    _currentSequence.Add(GenerateSequence(_round));

    OnRoundStarted(_round, GetInterval(_round));
    _showRoutine = StartCoroutine(ShowSequenceRoutine());
  }

  private IEnumerator ShowSequenceRoutine()
  {
    _state = GameState.ShowingSequence;

    float interval = GetInterval(_round);

    yield return new WaitForSeconds(_delayBeforeSequence);

    foreach (int index in _currentSequence.Current)
    {
      PopupButton(index, interval * _popupRatio);
      yield return new WaitForSeconds(interval);
    }

    _showRoutine = null;
    _state = GameState.WaitingInput;
  }

  private void OnButtonPressed(int index)
  {
    if (_state != GameState.WaitingInput)
      return;

    PopupButton(index, _inputFeedbackDuration);

    List<int> sequence = _currentSequence.Current;

    if (index != sequence[_inputIndex])
    {
      HandleMistake();
      return;
    }

    _inputIndex++;

    if (_inputIndex >= sequence.Count)
      CompleteRound();
  }

  private void CompleteRound()
  {
    _roundsCompleted++;
    OnRoundCompleted(_round);

    if (IsLastRound(_round))
      Finish(won: true);
    else
      NextRound();
  }

  private void Finish(bool won)
  {
    _state = GameState.Finished;

    if (won)
      OnGameWon();
    else
      OnGameOver(_roundsCompleted);

    if (HasProgressToSave(won) && _savePanel != null)
      OpenSavePrompt();
    else
      StartReturnTimeout(ReturnToMenu);
  }

  private void StopShowRoutine()
  {
    if (_showRoutine == null)
      return;

    StopCoroutine(_showRoutine);
    _showRoutine = null;
  }

  private void OpenSavePrompt()
  {
    _savePromptOpen = true;

    _nameInput.SetTextWithoutNotify(string.Empty);
    _nameInput.characterLimit = MAXNAMELENGTH;
    _confirmSaveButton.interactable = false;

    _promptTween?.Kill();
    _promptTween = _savePopup.Show(_savePanel).SetLink(gameObject);

    StartReturnTimeout(EndSavePrompt);
  }

  private void EndSavePrompt()
  {
    if (!_savePromptOpen)
      return;

    _savePromptOpen = false;
    _timeoutTween?.Kill();

    _promptTween?.Kill();
    _promptTween = _savePopup.Hide(_savePanel, ReturnToMenu).SetLink(gameObject);
  }

  private void OnSaveConfirmed()
  {
    if (!_savePromptOpen)
      return;

    string playerName = _nameInput.text.Trim();

    if (playerName.Length == 0)
      return;

    DataManager.Instance.PlayerName = playerName;
    SaveProgress();
    EndSavePrompt();
  }

  private void OnNameEdited(string value)
  {
    if (!_savePromptOpen)
      return;

    _confirmSaveButton.interactable = !string.IsNullOrWhiteSpace(value);
    StartReturnTimeout(EndSavePrompt);
  }

  private void StartReturnTimeout(TweenCallback onTimeout)
  {
    _timeoutTween?.Kill();
    _timeoutTween = DOVirtual.DelayedCall(_returnToMenuTimeout, onTimeout).SetLink(gameObject);
  }

  private void HideSavePromptInstant()
  {
    _savePromptOpen = false;
    _timeoutTween?.Kill();
    _promptTween?.Kill();

    if (_savePanel != null)
      _savePanel.gameObject.SetActive(false);
  }

  private void ReturnToMenu()
  {
    if (_menuManager == null)
      _menuManager = FindFirstObjectByType<MenuManager>();

    if (_menuManager != null)
      _menuManager.GoToMainMenu();
  }

  private void BindSavePrompt()
  {
    UnbindSavePrompt();

    if (_savePanel == null)
      return;

    _confirmSaveButton.onClick.AddListener(OnSaveConfirmed);
    _declineSaveButton.onClick.AddListener(EndSavePrompt);
    _nameInput.onValueChanged.AddListener(OnNameEdited);
  }

  private void UnbindSavePrompt()
  {
    if (_savePanel == null)
      return;

    _confirmSaveButton.onClick.RemoveListener(OnSaveConfirmed);
    _declineSaveButton.onClick.RemoveListener(EndSavePrompt);
    _nameInput.onValueChanged.RemoveListener(OnNameEdited);
  }

  protected Sequence PopupButton(int index, float duration = 1f)
  {
    Transform target = _buttons[index].transform;
    TextMeshProUGUI textMesh = target.GetComponentInChildren<TextMeshProUGUI>();

    target.DOKill(true);
    target.localScale = Vector3.one;

    Color originalColor = textMesh.color;
    float half = duration * .5f;

    return DOTween
        .Sequence()
        .SetTarget(target)
        .Append(target.DOScale(Vector3.one * 1.25f, half))
        .Join(textMesh.DOColor(_flashColor, half))
        .Append(target.DOScale(Vector3.one, half))
        .Join(textMesh.DOColor(originalColor, half));
  }

  private void ResetButtonVisuals()
  {
    foreach (GameObject button in _buttons)
    {
      button.transform.DOKill();
      button.transform.localScale = Vector3.one;
    }
  }

  private void BindButtons()
  {
    UnbindButtons();

    for (int i = 0; i < _buttons.Count; i++)
    {
      Button button = _buttons[i].GetComponent<Button>();
      if (button == null)
        continue;

      int index = i;
      UnityAction action = () => OnButtonPressed(index);
      button.onClick.AddListener(action);
      _bindings.Add((button, action));
    }
  }

  private void UnbindButtons()
  {
    foreach (var (button, action) in _bindings)
      if (button != null)
        button.onClick.RemoveListener(action);

    _bindings.Clear();
  }

  private void NotifyLivesChanged()
  {
    if (_livesOutput != null)
      _livesOutput.text = $"{_lives}/{_maxLives}";

    OnLivesChanged(_lives, _maxLives);
  }

  private void FlashLives()
  {
    if (_livesOutput == null)
      return;

    if (!_livesBaseColorCaptured)
    {
      _livesBaseColor = _livesOutput.color;
      _livesBaseColorCaptured = true;
    }

    _damageTween?.Kill();
    _livesOutput.color = _livesBaseColor;

    _damageTween = _livesOutput
        .DOColor(_damageFlashColor, _damageFlashDuration * 0.5f)
        .SetLoops(2, LoopType.Yoyo)
        .SetLink(gameObject);
  }

  private void ResetLivesVisual()
  {
    _damageTween?.Kill();

    if (_livesOutput != null && _livesBaseColorCaptured)
      _livesOutput.color = _livesBaseColor;
  }
}
