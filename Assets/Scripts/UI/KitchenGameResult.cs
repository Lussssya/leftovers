using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Leftovers
{
    public class KitchenGameResult : MonoBehaviour
    {
        public string menuScene = "Intro";
        public float tableSettleSeconds = .25f;
        public bool Finished { get; private set; }
        public bool Won { get; private set; }
        public float ElapsedSeconds { get; private set; }
        Microwave[] microwaves;
        MicrowaveFood[] foods;
        KitchenNoise kitchenNoise;
        NoiseBarUI noiseBar;
        Collider table;
        float settled;
        bool ready;
        PanelSettings panel;
        string gameScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterScenes()
        {
            SceneManager.sceneLoaded -= AddToGame;
            SceneManager.sceneLoaded += AddToGame;
        }

        static void AddToGame(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Environment") return;
            if (FindObjectOfType<KitchenGameResult>() != null) return;
            new GameObject("Win and lose").AddComponent<KitchenGameResult>();
        }

        IEnumerator Start()
        {
            gameScene = gameObject.scene.name;
            while (FindObjectOfType<KitchenIntro>() != null) yield return null;
            microwaves = FindObjectsOfType<Microwave>();
            foods = FindObjectsOfType<MicrowaveFood>(true);
            kitchenNoise = FindObjectOfType<KitchenNoise>();
            if (kitchenNoise != null)
            {
                noiseBar = kitchenNoise.GetComponent<NoiseBarUI>();
            }
            GameObject tabletop = GameObject.Find("Dining tabletop");
            if (tabletop != null) table = tabletop.GetComponent<Collider>();
            ready = true;
        }

        void LateUpdate()
        {
            if (!ready || Finished || Time.timeScale == 0f) return;
            ElapsedSeconds += Time.deltaTime;
            if (kitchenNoise != null && kitchenNoise.LimitReached)
            {
                if (noiseBar == null || !noiseBar.isActiveAndEnabled || noiseBar.IsFull)
                {
                    EndGame(false, "Too much noise woke someone up.");
                }
                return;
            }
            bool served = false;
            foreach (MicrowaveFood food in foods)
            {
                if (FoodOnTable(food)) served = true;
            }
            if (served) settled += Time.deltaTime;
            else settled = 0f;
            if (served && settled >= tableSettleSeconds) EndGame(true, "Midnight snack secured.");
        }

        bool FoodOnTable(MicrowaveFood food)
        {
            if (food == null || !food.isActiveAndEnabled || table == null || food.IsHeld || !food.IsCooked || food.heatedSeconds <= 0f) return false;
            foreach (Microwave microwave in microwaves)
            {
                if (microwave.food == food) return false;
            }
            Rigidbody body = food.GetComponent<Rigidbody>();
            if (body != null && body.velocity.sqrMagnitude > .04f) return false;
            Bounds bounds = new Bounds();
            bool found = false;
            foreach (BoxCollider part in food.GetComponentsInChildren<BoxCollider>())
            {
                if (!part.enabled || part.isTrigger) continue;
                if (!found) { bounds = part.bounds; found = true; }
                else bounds.Encapsulate(part.bounds);
            }
            if (!found) return false;
            Bounds surface = table.bounds;
            if (bounds.center.x < surface.min.x || bounds.center.x > surface.max.x) return false;
            if (bounds.center.z < surface.min.z || bounds.center.z > surface.max.z) return false;
            return Mathf.Abs(bounds.min.y - surface.max.y) < .12f;
        }

        public void ServeFood(MicrowaveFood food)
        {
            if (kitchenNoise != null && kitchenNoise.LimitReached)
            {
                return;
            }

            if (ready && !Finished && FoodOnTable(food))
                EndGame(true, "Midnight snack secured.");
        }

        void EndGame(bool won, string reason)
        {
            if (Finished) return;
            Finished = true;
            Won = won;
            foreach (FirstPersonController player in FindObjectsOfType<FirstPersonController>())
            {
                player.SetPaused(true);
                player.enabled = false;
            }
            foreach (MouseDoorInteractor input in FindObjectsOfType<MouseDoorInteractor>()) input.enabled = false;
            foreach (MouseFoodInteractor input in FindObjectsOfType<MouseFoodInteractor>()) input.enabled = false;
            foreach (MouseCabinetInteractor input in FindObjectsOfType<MouseCabinetInteractor>()) input.enabled = false;
            foreach (ProductInteractor products in FindObjectsOfType<ProductInteractor>()) products.enabled = false;
            foreach (KitchenHud hud in FindObjectsOfType<KitchenHud>()) hud.enabled = false;
            foreach (SandwichAssembly assembly in FindObjectsOfType<SandwichAssembly>()) assembly.enabled = false;
            foreach (Canvas canvas in FindObjectsOfType<Canvas>()) canvas.enabled = false;
            Time.timeScale = 0f;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            MakeScreen(reason);
        }

        void MakeScreen(string reason)
        {
            UIDocument document = gameObject.AddComponent<UIDocument>();
            panel = Instantiate(Resources.Load<PanelSettings>("CrosshairPanel"));
            document.panelSettings = panel;
            document.sortingOrder = 500;
            VisualElement root = document.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = 0; root.style.right = 0; root.style.top = 0; root.style.bottom = 0;
            root.style.backgroundColor = new Color(.015f, .02f, .03f, .94f);
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;
            string heading = "YOU LOST";
            if (Won) heading = "YOU WIN";
            Label title = new Label(heading);
            title.style.fontSize = 54;
            title.style.color = new Color(.95f, .92f, .82f);
            root.Add(title);
            Label detail = new Label(reason);
            detail.style.color = Color.white;
            detail.style.fontSize = 20;
            detail.style.marginBottom = 20;
            root.Add(detail);
            if (Won)
            {
                int minutes = Mathf.FloorToInt(ElapsedSeconds / 60f);
                float seconds = ElapsedSeconds - minutes * 60;
                Label time = new Label("Your time: " + minutes.ToString("00") + ":" + seconds.ToString("00.00"));
                time.style.color = Color.white;
                time.style.fontSize = 26;
                time.style.marginBottom = 24;
                root.Add(time);
            }
            else
            {
                Button restart = new Button(Restart) { text = "RESTART", name = "Restart" };
                StyleButton(restart);
                root.Add(restart);
            }
            Button menu = new Button(MainMenu) { text = "MAIN MENU", name = "MainMenu" };
            StyleButton(menu);
            root.Add(menu);
        }

        void StyleButton(Button button)
        {
            button.style.width = 250;
            button.style.height = 48;
            button.style.marginTop = 10;
            button.style.fontSize = 20;
            button.style.color = Color.white;
            button.style.backgroundColor = new Color(.1f, .12f, .15f);
        }

        void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameScene);
        }

        void MainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuScene);
        }

        void OnDestroy()
        {
            if (panel != null) Destroy(panel);
        }
    }
}


