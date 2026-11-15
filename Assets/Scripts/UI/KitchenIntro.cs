using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Leftovers
{
    public class KitchenIntro : MonoBehaviour
    {
        public string gameplayScene = "Environment";
        [Min(0f), InspectorName("Camera Move Duration (Seconds)")]
        public float cameraMoveSeconds = 0.45f;
        [Range(0f, 120f), InspectorName("Door Opening Angle")]
        public float openingAngle = 45f;
        [Min(0f), InspectorName("Heating Duration (Seconds)")]
        public float titleSeconds = 3f;
        [Min(0f)] public float startBeepDelaySeconds = .3f;
        [Min(0f)] public float blackoutFadeSeconds = 0f;
        [Min(0f)] public float logoFadeInSeconds = 2.4f;
        [Tooltip("These delays begin when the finish recording starts.")]
        [Min(0f)] public float logoPoofDelaySeconds = .02f;
        [Min(0f)] public float logoPoofDurationSeconds = 1f;
        [Min(0f)] public float gameRevealDelaySeconds = 1.02f;
        [Min(0f)] public float gameRevealDurationSeconds = 3f;
        [Min(0f)] public float beepFadeDelaySeconds = 1.02f;
        [Min(0f)] public float beepFadeDurationSeconds = 3f;
        [Min(0f)] public float muffleDelaySeconds = .12f;
        [Min(0f)] public float muffleDurationSeconds = 1.8f;
        [Range(10f, 22000f)] public float clearSoundFrequency = 22000f;
        [Range(10f, 22000f)] public float muffledSoundFrequency = 160f;
        [Range(0f, 1f)] public float humVolume = .38f;
        [Range(0f, 1f)] public float startBeepVolume = .55f;
        [Range(0f, 2f)] public float finishBeepVolumeMultiplier = 1f;
        public Vector3 menuCameraOffset = new Vector3(0f, .65f, 3.1f);
        public Vector3 menuLookOffset = new Vector3(0f, .6f, 0f);
        public Vector3 tutorialCameraOffset = new Vector3(-1.8f, .18f, 1.4f);
        public Vector3 tutorialLookOffset = new Vector3(-.3f, 0f, 0f);
        public Vector2 paperSize = new Vector2(.65f, .65f);
        public Color paperTint = Color.white;
        [Range(1f, 100f)] public float logoWidthPercent = 85f;
        [Range(1f, 100f)] public float logoHeightPercent = 42f;
        [Tooltip("Interior movement stays inside the letter shapes. Zero stops it.")]
        [Range(0f, 1f)] public float cloudMovementAmount = .85f;
        [Min(0f)] public float cloudMovementSpeed = 1f;
        [Min(0f)] public float cloudTurnDegreesPerSecond = 12f;
        [Range(0f, 1f)] public float cloudTextureMotionAmount = .35f;
        [Min(0f)] public float cloudTextureMotionSpeed = .35f;
        [Range(.5f, 2f)] public float cloudSize = 1f;
        public Color cloudColor = Color.white;
        [Min(0f)] public float cloudPoofDistance = 1f;
        [Min(0f)] public float cloudPoofExpansion = 2.5f;
        [Min(0f)] public float cloudPoofCurl = .15f;
        [Range(0f, .9f)] public float cloudPoofStagger = .12f;
        [Min(.1f)] public float doorSpringDurationSeconds = .7f;
        [Range(0f, .5f)] public float doorSpringOvershoot = .18f;
        [Range(0f, .4f)] public float cloudEdgeDrift = .12f;
        [Range(0f, 3f)] public float cloudDepth = 1.8f;
        public Vector3 cloudViewAngle = new Vector3(-8f, -16f, 0f);
        [Min(.1f)] public float menuViewMoveSeconds = .75f;
        [Range(0f, 110f)] public float optionsDoorAngle = 85f;
        [Range(0f, 1f)] public float helpDrawerOpenFraction = .85f;
        public Vector3 optionsCameraOffset = new Vector3(0f, .05f, 1.7f);
        public Vector3 helpCameraOffset = new Vector3(0f, .9f, 1.35f);
        [Min(0f)] public float menuSettleSeconds = .15f;
        [Range(0f, 1f)] public float backArrowOpacity = .7f;
        [Min(.1f)] public float backArrowSize = .7f;
        public Vector3 playBackArrowViewport = new Vector3(.12f, .5f, .65f);
        public Vector3 stepBackArrowViewport = new Vector3(.5f, .28f, .65f);
        public Vector3 playArrowPosition = new Vector3(3.726132f, 2.399229f, 1.566865f);
        public Vector3 playArrowRotation = new Vector3(5.014f, 107.23f, 0f);
        public Vector3 arrowModelScale = new Vector3(.12f, .12f, .12f);
        public Color backArrowHoverColor = new Color(1f, .92f, .62f, 1f);
        GameObject curvedBackArrow;
        GameObject straightBackArrow;
        GameObject activeBackArrow;
        Material backArrowMaterial;
        VisualElement optionsPanel;
        CabinetPart[] menuCabinetParts;
        Vector3 menuPosition;
        Quaternion menuRotation;
        bool menuMoving;
        float defaultSensitivity = 2f;
        Camera view;
        Microwave microwave;
        MouseDoorInteractor input;
        VisualElement screen;
        VisualElement menu;
        VisualElement handleCursor;
        IntroCloudLogo cloudLogo;
        VisualElement logoImage;
        PanelSettings panel;
        bool waiting;
        bool started;
        AudioSource motor;
        AudioSource beep;
        GameObject note;
        List<Behaviour> pausedControls = new List<Behaviour>();
        float gameplayTimeScale;
        bool loadingGameplay;

        void Start()
        {
            Time.timeScale = 1f;
            view = Camera.main;
            microwave = FindObjectOfType<Microwave>();
            if (view == null || microwave == null) { enabled = false; return; }
            FirstPersonController player = FindObjectOfType<FirstPersonController>();
            if (player != null)
            {
                defaultSensitivity = player.mouseSensitivity;
                player.SetPaused(false);
                player.enabled = false;
                Rigidbody body = player.GetComponent<Rigidbody>();
                body.isKinematic = true;
                view.transform.SetParent(null, true);
                player.gameObject.SetActive(false);
            }
            foreach (CameraFollow follow in FindObjectsOfType<CameraFollow>()) follow.enabled = false;
            foreach (MouseFoodInteractor food in FindObjectsOfType<MouseFoodInteractor>()) food.enabled = false;
            foreach (MouseCabinetInteractor cabinet in FindObjectsOfType<MouseCabinetInteractor>()) cabinet.enabled = false;
            input = view.GetComponent<MouseDoorInteractor>();
            input.enabled = false;
            input.useCenterAim = false;
            input.followHandleArc = true;
            input.interactionOrigin = view.transform;

            microwave.enabled = false;
            microwave.door.SoundEmitted += DoorSound;
            motor = microwave.cookingSound;
            beep = microwave.beepSource;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            Bounds bounds = GetBounds(microwave.gameObject);
            view.transform.position = bounds.center + menuCameraOffset;
            view.transform.LookAt(bounds.center + menuLookOffset);

            foreach (KitchenHud hud in FindObjectsOfType<KitchenHud>()) hud.enabled = false;
            foreach (ProductInteractor products in FindObjectsOfType<ProductInteractor>()) products.enabled = false;
            foreach (SandwichAssembly assembly in FindObjectsOfType<SandwichAssembly>()) assembly.enabled = false;
            foreach (Canvas canvas in FindObjectsOfType<Canvas>()) canvas.enabled = false;
            MakeNote(bounds);
            MakeMenu();
            menuPosition = view.transform.position;
            menuRotation = view.transform.rotation;
            GameObject lowerCabinet = GameObject.Find("Microwave cabinet");
            if (lowerCabinet != null) menuCabinetParts = lowerCabinet.GetComponentsInChildren<CabinetPart>();
            else menuCabinetParts = new CabinetPart[0];
            if (PlayerPrefs.HasKey("Leftovers.SoundVolume"))
                AudioListener.volume = PlayerPrefs.GetFloat("Leftovers.SoundVolume");
        }

        Bounds GetBounds(GameObject item)
        {
            Bounds bounds = new Bounds(item.transform.position, Vector3.zero);
            foreach (MeshRenderer renderer in item.GetComponentsInChildren<MeshRenderer>()) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        void MakeMenu()
        {
            UIDocument document = gameObject.AddComponent<UIDocument>();
            panel = Instantiate(Resources.Load<PanelSettings>("CrosshairPanel"));
            document.panelSettings = panel;
            document.sortingOrder = 200;
            VisualElement root = document.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = 0; root.style.right = 0; root.style.top = 0; root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore;
            menu = new VisualElement();
            menu.style.position = Position.Absolute;
            menu.style.left = Length.Percent(8); menu.style.bottom = Length.Percent(12);
            menu.style.width = 230;
            root.Add(menu);
            Label caption = new Label("3:00 AM.  ONE LAST BITE.");
            caption.style.color = new Color(.85f,.83f,.73f);
            caption.style.fontSize = 16; caption.style.marginBottom = 15;
            menu.Add(caption);
            Button play = new Button(Play) { text = "PLAY" };
            Button options = new Button(OpenOptions) { text = "OPTIONS", name = "Options" };
            Button help = new Button(OpenHelp) { text = "HELP", name = "Help" };
            Button exit = new Button(ExitGame) { text = "EXIT", name = "Exit" };
            foreach (Button button in new Button[] {play, options, help, exit})
            {
                button.style.height = 46; button.style.marginBottom = 8;
                button.style.backgroundColor = new Color(.08f,.1f,.12f,.9f);
                button.style.color = new Color(.95f,.92f,.82f);
                button.style.fontSize = 17;
                menu.Add(button);
            }
            MakeMenuViews(root);
            screen = new VisualElement();
            screen.style.position = Position.Absolute;
            screen.style.left = 0; screen.style.right = 0; screen.style.top = 0; screen.style.bottom = 0;
            screen.style.backgroundColor = Color.black;
            screen.style.justifyContent = Justify.Center;
            screen.style.alignItems = Align.Center;
            screen.style.opacity = 0;
            screen.pickingMode = PickingMode.Ignore;
            screen.style.display = DisplayStyle.None;
            GameObject logoObject = new GameObject("Intro cloud logo");
            logoObject.transform.SetParent(transform, false);
            logoObject.transform.position = new Vector3(10000f, 10000f, 10000f);
            cloudLogo = logoObject.AddComponent<IntroCloudLogo>();
            cloudLogo.settings = this;
            cloudLogo.Build();
            logoImage = new VisualElement();
            logoImage.pickingMode = PickingMode.Ignore;
            logoImage.style.scale = new Scale(new Vector3(IntroCloudLogo.RenderPadding, IntroCloudLogo.RenderPadding, 1f));
            logoImage.style.overflow = Overflow.Visible;
            logoImage.style.width = Length.Percent(logoWidthPercent);
            logoImage.style.height = Length.Percent(logoHeightPercent);
            logoImage.style.backgroundImage = Background.FromRenderTexture(cloudLogo.texture);
            logoImage.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            screen.Add(logoImage);
            root.Add(screen);
            handleCursor = new VisualElement();
            handleCursor.pickingMode = PickingMode.Ignore;
            handleCursor.style.position = Position.Absolute;
            handleCursor.style.width = 8f;
            handleCursor.style.height = 8f;
            handleCursor.style.backgroundColor = Color.white;
            handleCursor.style.borderTopLeftRadius = 4f;
            handleCursor.style.borderTopRightRadius = 4f;
            handleCursor.style.borderBottomLeftRadius = 4f;
            handleCursor.style.borderBottomRightRadius = 4f;
            handleCursor.style.display = DisplayStyle.None;
            root.Add(handleCursor);
        }

        void LateUpdate()
        {
            if (handleCursor == null) return;
            if (!waiting || input == null || input.Held == null)
            {
                handleCursor.style.display = DisplayStyle.None;
                if (waiting) UnityEngine.Cursor.visible = true;
                return;
            }
            UnityEngine.Cursor.visible = false;
            Vector3 point = view.WorldToScreenPoint(input.HeldPoint);
            point.y = Screen.height - point.y;
            Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(handleCursor.panel, point);
            handleCursor.style.left = panelPoint.x - 4f;
            handleCursor.style.top = panelPoint.y - 4f;
            handleCursor.style.display = DisplayStyle.Flex;
        }

        void Play()
        {
            if (started || menuMoving) return;
            menuMoving = true;
            started = true;
            menu.style.display = DisplayStyle.None;
            StartCoroutine(PrepareDoor());
        }

        IEnumerator PrepareDoor()
        {
            Bounds bounds = GetBounds(microwave.gameObject);
            Vector3 start = view.transform.position;
            Quaternion rotation = view.transform.rotation;
            Vector3 end = bounds.center + tutorialCameraOffset;
            Quaternion target = Quaternion.LookRotation(bounds.center + tutorialLookOffset - end);
            microwave.door.BeginHold();

            for(float t=0; t<Mathf.Max(cameraMoveSeconds, doorSpringDurationSeconds); t+=Time.unscaledDeltaTime)
            {
                float blend = Mathf.SmoothStep(0, 1, Progress(t, 0f, cameraMoveSeconds));
                float springTime = Progress(t, 0f, doorSpringDurationSeconds);
                float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(springTime / .55f));
                float bounce = Mathf.Sin(Mathf.Clamp01((springTime - .35f) / .65f) * Mathf.PI);
                microwave.door.HoldAtAngle(openingAngle * (rise + bounce * doorSpringOvershoot));
                view.transform.position = Vector3.Lerp(start,end,blend);
                view.transform.rotation = Quaternion.Slerp(rotation,target,blend);
                yield return null;
            }
            view.transform.SetPositionAndRotation(end,target);
            microwave.door.HoldAtAngle(openingAngle);
            float settleTime = 0f;
            while (Mathf.Abs(microwave.door.Angle - openingAngle) > .1f && settleTime < doorSpringDurationSeconds)
            {
                settleTime += Time.unscaledDeltaTime;
                yield return null;
            }
            microwave.door.EndHold(true);
            input.enabled = true;
            waiting = true;
            menuMoving = false;
            ShowBackArrow(false);
        }

        void DoorSound(FridgeDoor door, DoorSoundKind kind, float strength)
        {
            if (!waiting || kind != DoorSoundKind.Slam) return;
            waiting = false;
            HideBackArrow();
            input.enabled = false;
            StartCoroutine(StartGame());
        }

        IEnumerator StartGame()
        {
            screen.style.display = DisplayStyle.Flex;
            StartCoroutine(FadeToBlack());
            UnityEngine.Cursor.visible = false;
            AudioClip finishClip = Resources.Load<AudioClip>("IntroLogo/Microwave_IntroFinish");
            AudioSource transitionSound = MakeTransitionSound(beep, "Intro microwave audio");
            AudioSource introHum = MakeTransitionSound(motor, "Intro microwave hum");
            introHum.clip = motor.clip;
            AudioLowPassFilter filter = transitionSound.gameObject.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = clearSoundFrequency;
            float volume = transitionSound.volume;
            yield return new WaitForSecondsRealtime(startBeepDelaySeconds);
            if (microwave.startBeep != null)
                transitionSound.PlayOneShot(microwave.startBeep, startBeepVolume);
            if (motor != null)
            {
                introHum.volume = humVolume;
                introHum.loop = true;
                introHum.Play();
            }
            for (float time = 0; time < Mathf.Max(titleSeconds, logoFadeInSeconds, blackoutFadeSeconds - startBeepDelaySeconds); time += Time.unscaledDeltaTime)
            {
                cloudLogo.Show(Mathf.SmoothStep(0f, 1f, Progress(time, 0f, logoFadeInSeconds)), 0f);
                yield return null;
            }
            cloudLogo.Show(1f, 0f);
            microwave.door.SoundEmitted -= DoorSound;
            DontDestroyOnLoad(gameObject);
            gameplayTimeScale = Time.timeScale;
            loadingGameplay = true;
            SceneManager.sceneLoaded += PauseGameplay;
            AsyncOperation loading = SceneManager.LoadSceneAsync(gameplayScene);
            while (!loading.isDone)
            {
                cloudLogo.Show(1f, 0f);
                yield return null;
            }
            SceneManager.sceneLoaded -= PauseGameplay;

            volume *= finishBeepVolumeMultiplier;
            transitionSound.volume = volume;
            transitionSound.clip = finishClip;
            introHum.Stop();
            transitionSound.Play();
            double soundStarted = AudioSettings.dspTime;
            float timePassed = 0f;
            float endTime = Mathf.Max(gameRevealDelaySeconds + gameRevealDurationSeconds,
                beepFadeDelaySeconds + beepFadeDurationSeconds,
                logoPoofDelaySeconds + logoPoofDurationSeconds,
                muffleDelaySeconds + muffleDurationSeconds);
            while (timePassed < endTime)
            {
                timePassed = (float)(AudioSettings.dspTime - soundStarted);
                float poof = Progress(timePassed, logoPoofDelaySeconds, logoPoofDurationSeconds);
                cloudLogo.Show(1f, poof);
                float fade = Progress(timePassed, gameRevealDelaySeconds, gameRevealDurationSeconds);
                screen.style.opacity = 1f - Mathf.SmoothStep(0f, 1f, fade);
                float audioFade = Progress(timePassed, beepFadeDelaySeconds, beepFadeDurationSeconds);
                transitionSound.volume = volume * (1f - Mathf.SmoothStep(0f, 1f, audioFade));
                float muffle = Progress(timePassed, muffleDelaySeconds, muffleDurationSeconds);
                filter.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(Mathf.Max(10f, clearSoundFrequency)), Mathf.Log(Mathf.Max(10f, muffledSoundFrequency)), muffle));
                yield return null;
            }
            transitionSound.Stop();
            screen.style.opacity = 0;
            ResumeGameplay();
            Destroy(gameObject);
        }

        float Progress(float time, float delay, float duration)
        {
            if (time < delay) return 0f;
            if (duration <= 0f) return 1f;
            return Mathf.Clamp01((time - delay) / duration);
        }

        IEnumerator FadeToBlack()
        {
            for (float time = 0f; time < blackoutFadeSeconds; time += Time.unscaledDeltaTime)
            {
                screen.style.opacity = Progress(time, 0f, blackoutFadeSeconds);
                yield return null;
            }
            screen.style.opacity = 1f;
        }

        AudioSource MakeTransitionSound(AudioSource original, string soundName)
        {
            GameObject sound = new GameObject(soundName);
            sound.transform.SetParent(transform);
            sound.transform.position = original.transform.position;
            AudioSource source = sound.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = original.volume;
            source.pitch = 1f;
            source.spatialBlend = original.spatialBlend;
            source.minDistance = original.minDistance;
            source.maxDistance = original.maxDistance;
            source.rolloffMode = original.rolloffMode;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
                original.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
            source.outputAudioMixerGroup = original.outputAudioMixerGroup;
            return source;
        }

        void PauseGameplay(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != gameplayScene) return;
            foreach (FirstPersonController player in FindObjectsOfType<FirstPersonController>())
            {
                if (PlayerPrefs.HasKey("Leftovers.MouseSensitivity"))
                    player.mouseSensitivity = PlayerPrefs.GetFloat("Leftovers.MouseSensitivity");
                PauseControl(player);
            }
            foreach (MouseDoorInteractor doors in FindObjectsOfType<MouseDoorInteractor>())
                PauseControl(doors);
            foreach (MouseFoodInteractor food in FindObjectsOfType<MouseFoodInteractor>())
                PauseControl(food);
            foreach (MouseCabinetInteractor cabinets in FindObjectsOfType<MouseCabinetInteractor>())
                PauseControl(cabinets);
            foreach (ProductInteractor products in FindObjectsOfType<ProductInteractor>()) PauseControl(products);
            foreach (KitchenHud hud in FindObjectsOfType<KitchenHud>()) PauseControl(hud);
            foreach (SandwichAssembly assembly in FindObjectsOfType<SandwichAssembly>()) PauseControl(assembly);
            foreach (Canvas canvas in FindObjectsOfType<Canvas>()) PauseControl(canvas);
            Time.timeScale = 0f;
        }

        void PauseControl(Behaviour control)
        {
            if (!control.enabled) return;
            pausedControls.Add(control);
            control.enabled = false;
        }

        void ResumeGameplay()
        {
            if (!loadingGameplay) return;
            loadingGameplay = false;
            Time.timeScale = gameplayTimeScale;
            foreach (Behaviour control in pausedControls)
            {
                if (control != null) control.enabled = true;
            }
            pausedControls.Clear();
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        void MakeNote(Bounds bounds)
        {
            note = GameObject.CreatePrimitive(PrimitiveType.Quad);
            note.name = "Microwave tutorial label";
            note.transform.position = new Vector3(bounds.min.x - .012f, bounds.center.y, bounds.center.z);
            note.transform.rotation = Quaternion.LookRotation(Vector3.right);
            note.transform.localScale = new Vector3(paperSize.x, paperSize.y, 1f);
            Material paper = new Material(Shader.Find("Standard"));
            paper.color = paperTint;
            paper.SetFloat("_Glossiness", 0f);
            paper.SetFloat("_Metallic", 0f);
            paper.mainTexture = Resources.Load<Texture2D>("MicrowaveTutorial");
            note.GetComponent<Renderer>().material = paper;
            Destroy(note.GetComponent<Collider>());
        }
        void MakeBackArrows()
        {
            backArrowMaterial = new Material(Resources.Load<Shader>("IntroArrows/RoomArrow"));
            backArrowMaterial.color = new Color(1f, 1f, 1f, backArrowOpacity);
            backArrowMaterial.SetFloat("_Glossiness", .42f);
            curvedBackArrow = MakeBackArrow("CurvedBack");

        }

        GameObject MakeBackArrow(string modelName)
        {
            GameObject arrow = new GameObject(modelName + " arrow");
            GameObject model = Instantiate(Resources.Load<GameObject>("IntroArrows/" + modelName), arrow.transform);
            model.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            model.transform.localScale = new Vector3(-1f, 1f, 1f);
            foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material = backArrowMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                MeshCollider collider = renderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            }
            arrow.SetActive(false);
            return arrow;
        }

        void ShowBackArrow(bool stepBack)
        {
            HideBackArrow();
            activeBackArrow = curvedBackArrow;
            activeBackArrow.transform.position = playArrowPosition;
            activeBackArrow.transform.rotation = Quaternion.Euler(playArrowRotation);
            if (stepBack)
            {
                Vector3 placement = stepBackArrowViewport;
                placement.x = .5f;
                activeBackArrow.transform.position = view.ViewportToWorldPoint(placement);
                activeBackArrow.transform.rotation = view.transform.rotation;
            }
            activeBackArrow.transform.localScale = arrowModelScale;
            backArrowMaterial.color = new Color(1f, 1f, 1f, backArrowOpacity);
            activeBackArrow.SetActive(true);
        }

        void HideBackArrow()
        {
            if (curvedBackArrow != null) curvedBackArrow.SetActive(false);
            if (straightBackArrow != null) straightBackArrow.SetActive(false);
            activeBackArrow = null;
        }

        void Update()
        {
            if (menuMoving || activeBackArrow == null) return;
            Ray ray = view.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            bool hovered = Physics.Raycast(ray, out hit, 20f) && hit.transform.IsChildOf(activeBackArrow.transform);
            Color color = Color.white;
            if (hovered) color = backArrowHoverColor;
            color.a = backArrowOpacity;
            backArrowMaterial.color = color;
            if (hovered && Input.GetMouseButtonDown(0)) ReturnToMenu();
        }

        void MakeMenuViews(VisualElement root)
        {
            MakeBackArrows();
            optionsPanel = new VisualElement();
            optionsPanel.name = "Sound and sensitivity";
            optionsPanel.style.position = Position.Absolute;
            optionsPanel.style.left = Length.Percent(35);
            optionsPanel.style.top = Length.Percent(40);
            optionsPanel.style.width = Length.Percent(30);
            optionsPanel.style.paddingLeft = 22;
            optionsPanel.style.paddingRight = 22;
            optionsPanel.style.paddingTop = 18;
            optionsPanel.style.paddingBottom = 18;
            optionsPanel.style.backgroundColor = new Color(.035f, .04f, .045f, .85f);
            optionsPanel.style.color = Color.white;
            optionsPanel.Add(new Label("OPTIONS"));
            Slider volumeSlider = new Slider("Sound", 0f, 1f);
            volumeSlider.name = "Sound";
            volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Leftovers.SoundVolume", AudioListener.volume));
            volumeSlider.RegisterValueChangedCallback(ChangeVolume);
            volumeSlider.labelElement.style.color = Color.white;
            optionsPanel.Add(volumeSlider);
            Slider sensitivitySlider = new Slider("Sensitivity", .1f, 10f);
            sensitivitySlider.name = "Sensitivity";
            sensitivitySlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Leftovers.MouseSensitivity", defaultSensitivity));
            sensitivitySlider.RegisterValueChangedCallback(ChangeSensitivity);
            sensitivitySlider.labelElement.style.color = Color.white;
            optionsPanel.Add(sensitivitySlider);
            optionsPanel.style.display = DisplayStyle.None;
            root.Add(optionsPanel);
        }

        void ChangeVolume(ChangeEvent<float> change)
        {
            AudioListener.volume = change.newValue;
            PlayerPrefs.SetFloat("Leftovers.SoundVolume", change.newValue);
        }

        void ChangeSensitivity(ChangeEvent<float> change)
        {
            PlayerPrefs.SetFloat("Leftovers.MouseSensitivity", change.newValue);
        }

        void OpenOptions()
        {
            if (menuMoving || started) return;
            StartCoroutine(OpenCabinetView(false));
        }

        void OpenHelp()
        {
            if (menuMoving || started) return;
            StartCoroutine(OpenCabinetView(true));
        }

        IEnumerator OpenCabinetView(bool help)
        {
            menuMoving = true;
            menu.style.display = DisplayStyle.None;
            List<CabinetPart> parts = new List<CabinetPart>();
            foreach (CabinetPart part in menuCabinetParts)
            {
                if (part.slidesOut == help) parts.Add(part);
            }
            Bounds bounds = GetBounds(GameObject.Find("Microwave cabinet"));
            if (help && parts.Count > 0) bounds = GetBounds(parts[0].gameObject);
            Vector3 offset = optionsCameraOffset;
            if (help) offset = helpCameraOffset;
            Vector3 destination = bounds.center + offset;
            Quaternion target = Quaternion.LookRotation(bounds.center - destination);
            Vector3 start = view.transform.position;
            Quaternion rotation = view.transform.rotation;
            foreach (CabinetPart part in parts) part.BeginHold();
            for (float time = 0; time < menuViewMoveSeconds; time += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(time / menuViewMoveSeconds);
                float blend = Mathf.SmoothStep(0f, 1f, progress);
                float spring = blend + Mathf.Sin(progress * Mathf.PI) * doorSpringOvershoot;
                foreach (CabinetPart part in parts)
                {
                    float amount = optionsDoorAngle;
                    if (help) amount = part.maximumOpen * helpDrawerOpenFraction;
                    part.HoldAtOpen(amount * spring);
                }
                view.transform.SetPositionAndRotation(Vector3.Lerp(start, destination, blend),
                    Quaternion.Slerp(rotation, target, blend));
                yield return null;
            }
            view.transform.SetPositionAndRotation(destination, target);
            foreach (CabinetPart part in parts)
            {
                float amount = optionsDoorAngle;
                if (help) amount = part.maximumOpen * helpDrawerOpenFraction;
                part.HoldAtOpen(amount);
            }
            yield return new WaitForSecondsRealtime(menuSettleSeconds);
            foreach (CabinetPart part in parts) part.EndHold(true);
            if (!help) optionsPanel.style.display = DisplayStyle.Flex;
            ShowBackArrow(true);
            menuMoving = false;
        }

        void ReturnToMenu()
        {
            if (menuMoving) return;
            StartCoroutine(CloseMenuView());
        }

        IEnumerator CloseMenuView()
        {
            menuMoving = true;
            waiting = false;
            input.enabled = false;
            UnityEngine.Cursor.visible = true;
            HideBackArrow();
            optionsPanel.style.display = DisplayStyle.None;
            PlayerPrefs.Save();
            Vector3 start = view.transform.position;
            Quaternion rotation = view.transform.rotation;
            microwave.door.BeginHold();
            float doorAngle = microwave.door.Angle;
            float[] amounts = new float[menuCabinetParts.Length];
            for (int i = 0; i < menuCabinetParts.Length; i++)
            {
                amounts[i] = menuCabinetParts[i].OpenAmount;
                menuCabinetParts[i].BeginHold();
            }
            for (float time = 0; time < menuViewMoveSeconds; time += Time.unscaledDeltaTime)
            {
                float blend = Mathf.SmoothStep(0f, 1f, time / menuViewMoveSeconds);
                view.transform.SetPositionAndRotation(Vector3.Lerp(start, menuPosition, blend),
                    Quaternion.Slerp(rotation, menuRotation, blend));
                microwave.door.HoldAtAngle(doorAngle * (1f - blend));
                for (int i = 0; i < menuCabinetParts.Length; i++)
                    menuCabinetParts[i].HoldAtOpen(amounts[i] * (1f - blend));
                yield return null;
            }
            microwave.door.HoldAtAngle(0f);
            foreach (CabinetPart part in menuCabinetParts) part.HoldAtOpen(0f);
            view.transform.SetPositionAndRotation(menuPosition, menuRotation);
            yield return new WaitForSecondsRealtime(menuSettleSeconds);
            microwave.door.EndHold(true);
            foreach (CabinetPart part in menuCabinetParts) part.EndHold(true);
            started = false;
            menuMoving = false;
            menu.style.display = DisplayStyle.Flex;
        }

        void ExitGame()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= PauseGameplay;
            ResumeGameplay();
            if (curvedBackArrow != null) Destroy(curvedBackArrow);
            if (straightBackArrow != null) Destroy(straightBackArrow);
            if (backArrowMaterial != null) Destroy(backArrowMaterial);
            if(microwave != null && microwave.door != null) microwave.door.SoundEmitted -= DoorSound;
            if(panel != null) Destroy(panel);
        }
    }
}











