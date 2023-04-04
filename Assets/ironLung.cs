using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

public class ironLung : MonoBehaviour
{ //I have added way too many comments to this because I think it could be useful if you wanna try this sort of thing yourself and maybe a couple notes here and there just from me making everything, enjoy    -arceus
    public KMAudio Audio;
    public KMBombModule Module;
    public AudioSource Sounds;
    public KMBombInfo Bomb;
    public GameObject DirectionArrow; //Movement is through gameobjects, no movement would look LAME
    public GameObject[] CW;
    public GameObject[] CCW;
    public GameObject[] Up;
    public GameObject[] Down;
    public GameObject[] Proximity; //No moving here but it flashes by setting as true/false
    public GameObject Blackout; //Covers the module when crashing or submitting
    public TextMesh BlackoutText;
    public KMSelectable[] Direction;
    public KMSelectable[] Movement;
    public KMSelectable Photo;
    public TextMesh DirectionText;
    public TextMesh[] Positioning;
    public TextMesh[] Goal;
    int[] GoalPosition = new int[3];
    float Angle = 0f;
    float XPos = 500f;
    float YPos = 500f;

    int OxygenCounter; //I'd rather not subject people to this going off too many times
    int Map;
    string[] MapInUse;

    bool[] ActiveProxSensors = new bool[4]; //Helps with logging and, when TP is active, halting movement

    bool BackwardHeld = false; //Movement
    bool ForwardHeld = false;
    bool SolveIncoming = false;
    int SpeedCalc = 0;

    bool LeftTurnButtonHeld = false; //Turning
    bool RightTurnButtonHeld = false;
    float RotationCalc = 0;

    bool PreventingButtonsCommittingFloatation; //Silly name, yes, but it explains it pretty well

    static int moduleIdCounter = 1;
    int moduleId;
    bool ModuleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        Blackout.SetActive(false);
        BlackoutText.text = "";
        for (byte i = 0; i < Direction.Length; i++)
        {
            KMSelectable Turning = Direction[i];
            Turning.OnInteract += delegate
            {
                Rotation(Turning);
                return false;
            };
            Turning.OnInteractEnded += delegate
            {
                StopRotation(Turning);
            };
        }
        for (byte i = 0; i < Movement.Length; i++)
        {
            KMSelectable Moving = Movement[i];
            Moving.OnInteract += delegate
            {
                Motion(Moving);
                return false;
            };
            Moving.OnInteractEnded += delegate
            {
                StopMotion(Moving);
            };
        }
        Photo.OnInteract += delegate { Submit(); return false; };
        for (int i = 0; i < 4; i++)
            Proximity[i].SetActive(false);
        StartCoroutine(Oxygen());
        Map = UnityEngine.Random.Range(0, 4);
        switch (Map)
        {
            case 0: MapInUse = ironData.map1; break;
            case 1: MapInUse = ironData.map2; break;
            case 2: MapInUse = ironData.map3; break;
            case 3: MapInUse = ironData.map4; break;
        }
        Debug.LogFormat("[Iron Lung #{0}] The map in use is number {1}.", moduleId, Map + 1);
    }

    void Start()
    {
        StartCoroutine(GoalFinding()); //Finding a goal, yes it's a coroutine
        StartCoroutine(ProximityCheckNorth()); //These ones are always active, constantly checking if a wall is nearby
        StartCoroutine(ProximityCheckEast());
        StartCoroutine(ProximityCheckSouth());
        StartCoroutine(ProximityCheckWest());
    }

    void Rotation(KMSelectable Turning) //Handles the left and right buttons
    {
        if (!ModuleSolved)
        {
            Audio.PlaySoundAtTransform("click", transform);
            if ((Array.IndexOf(Direction, Turning)) % 2 == 0)
            {
                if (!RightTurnButtonHeld) //Extra check to prevent double taps breaking the buttons
                {
                    CW[0].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
                    CW[1].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
                }
                RightTurnButtonHeld = true;
            }
            else
            {
                if (!LeftTurnButtonHeld) //Same goes for here and the rotation buttons
                {
                    CCW[0].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
                    CCW[1].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
                }
                LeftTurnButtonHeld = true;
            }
        }
    }

    void StopRotation(KMSelectable Turning) //Released left and right buttons
    {
        if (PreventingButtonsCommittingFloatation) PreventingButtonsCommittingFloatation = false;
        else if (!ModuleSolved)
        {
            if ((Array.IndexOf(Direction, Turning)) % 2 == 0)
            {
                CW[0].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
                CW[1].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
            }
            else
            {
                CCW[0].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
                CCW[1].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
            }
            LeftTurnButtonHeld = false;
            RightTurnButtonHeld = false;
        }
    }

    void Motion(KMSelectable Moving) //Handles the up and down buttons
    {
        if (!ModuleSolved)
        {
            Audio.PlaySoundAtTransform("click", transform);
            if ((Array.IndexOf(Movement, Moving)) % 2 == 0)
            {
                if (!BackwardHeld)
                {
                    Down[0].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
                    Down[1].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
                }
                BackwardHeld = true;
            }
            else
            {
                if (!ForwardHeld)
                {
                    Up[0].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
                    Up[1].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
                }
                ForwardHeld = true;
            }
        }
    }

    void StopMotion(KMSelectable Moving) //Released up and down buttons
    {
        if (PreventingButtonsCommittingFloatation) PreventingButtonsCommittingFloatation = false;
        else if (!ModuleSolved)
        {
            if ((Array.IndexOf(Movement, Moving)) % 2 == 0)
            {
                Down[0].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
                Down[1].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
            }
            else
            {
                Up[0].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
                Up[1].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
            }
            BackwardHeld = false;
            ForwardHeld = false;
        }
    }

    void Submit() //funni submit button lmao
    {
        if (!ModuleSolved) //You might notice there's no movement commands, that's because the entire module gets covered up as soon as the button is pressed so there's no need for animating anything
        {
            ModuleSolved = true; //It isn't truly solved of course but this is easier than writing another check in the if statement
            Debug.LogFormat("[Iron Lung #{0}] You navigated to ({1},{2}), turned to an angle of {3}, and took a photo there.", moduleId, XPos.ToString("0.00"), YPos.ToString("0.00"), Angle.ToString("0.00"));
            Audio.PlaySoundAtTransform("submit", transform);
            Blackout.SetActive(true); //Semi-emulating the real game, picture frame is black for a few seconds before the image appears
            if (Math.Abs(GoalPosition[0] - XPos) <= 2 && Math.Abs(GoalPosition[1] - YPos) <= 2 && Math.Abs(GoalPosition[2] - Angle) <= 10)
                SolveIncoming = true; //For TP returning solves
            StartCoroutine(HoldOn());
        }
    }

    IEnumerator HoldOn() //Submission checker
    {
        yield return new WaitForSecondsRealtime(2.8f);
        float tempangle = Angle; //PREVENTING EDGE CASE ISSUES WITH BEING AT A VALID ANGLE BUT WITH DISTANT DIGITS
        if (GoalPosition[2] >= 350 && Angle <= 10) tempangle += 360;
        else if (GoalPosition[2] <= 10 && Angle >= 350) tempangle -= 360;
        if (Math.Abs(GoalPosition[0] - XPos) <= 2 && Math.Abs(GoalPosition[1] - YPos) <= 2 && Math.Abs(GoalPosition[2] - tempangle) <= 10)
        {
            BlackoutText.text = "SOLVED"; // your did it
            Debug.LogFormat("[Iron Lung #{0}] This is within range of your goal, and your training has been completed. Congratulations.", moduleId);
            Module.HandlePass();
            StopAllCoroutines(); //Stops prox sensors, this coroutine, and the oxygen thingy so it won't nag you after this is solved
        }
        else
        {
            BlackoutText.text = "WRONG"; //No reset occurs here, because why should it happen
            Debug.LogFormat("[Iron Lung #{0}] This is not close enough to your goal. A strike has been issued. Please try again.", moduleId);
            Module.HandleStrike();
            yield return new WaitForSeconds(3f);
            Blackout.SetActive(false);
            BlackoutText.text = "";
            ModuleSolved = false;
        }
    }

    private void FixedUpdate() //Almost all of this handles the momentum-like movements
    {
        if (ForwardHeld && SpeedCalc < 39) //Gradually increase the rate at which given units increase until eventually reaching a cap
            SpeedCalc++;
        else if (!ForwardHeld && SpeedCalc > 0) //Does the opposite when button is not held and speed is in that button's direction
            SpeedCalc--;
        if (BackwardHeld && SpeedCalc > -39)
            SpeedCalc--;
        else if (!BackwardHeld && SpeedCalc < 0)
            SpeedCalc++;
        if ((SpeedCalc == 1 || SpeedCalc == -1) && !BackwardHeld && !ForwardHeld) //Kinda unneeded but just to play it safe, this prevents constant movement despite no interaction
            SpeedCalc = 0;
        if (RightTurnButtonHeld && RotationCalc < 79)
            RotationCalc++;
        else if (!RightTurnButtonHeld && RotationCalc > 0) //Deceleration is twice as fast as acceleration for turning
            RotationCalc = RotationCalc - 2;
        if (LeftTurnButtonHeld && RotationCalc > -79)
            RotationCalc--;
        else if (!LeftTurnButtonHeld && RotationCalc < 0)
            RotationCalc = RotationCalc + 2;
        if ((RotationCalc == 1 || RotationCalc == -1) && !LeftTurnButtonHeld && !RightTurnButtonHeld)
            RotationCalc = 0;
        Angle = (Angle + (RotationCalc / 100f)) % 360f; //Angle calcs are really simple, add or subtract 0.01 units per rotation speed every tick
        if (Angle < 0)
            Angle = Angle + 360f; //You can't have a negative angle
        DirectionArrow.transform.Rotate(0.0f, 0.0f, (RotationCalc / 100f), Space.Self); //Moves around the directional arrow
        DirectionText.text = Angle.ToString("0.00");
        while (DirectionText.text.Length < 6)
            DirectionText.text = "0" + DirectionText.text;
        if (DirectionText.text == "360.00")
            DirectionText.text = "000.00";
        if (Angle >= 0 && Angle <= 180) //Lots of code just to determine how much each direction changes every tick
        {
            XPos = XPos + ((90 - (Math.Abs(Angle - 90f))) / 90f * SpeedCalc) / 300f; //I don't think this perfectly aligns with real life, but it's close-ish, and it works just fine anyway
        }
        else
        {
            XPos = XPos - ((90 - (Math.Abs(Angle - 270f))) / 90f * SpeedCalc) / 300f; //Fun fact, the end divisor (300f) took many many changes to find one that works the best
        }
        if (Angle >= 90 && Angle <= 270)
        {
            YPos = YPos - ((90 - (Math.Abs(Angle - 180f))) / 90f * SpeedCalc) / 300f;
        }
        else
        {
            YPos = YPos + (((Math.Abs(Angle - 180f)) - 90f) / 90f * SpeedCalc) / 300f;
        }
        Positioning[0].text = XPos.ToString("0.00");
        while (Positioning[0].text.Length < 6)
            Positioning[0].text = "0" + Positioning[0].text;
        Positioning[1].text = YPos.ToString("0.00");
        while (Positioning[1].text.Length < 6)
            Positioning[1].text = "0" + Positioning[1].text;
        if (MapInUse[99 - ((int)YPos / 10)].Substring(((int)XPos / 10), 1) == "1") { Debug.LogFormat("[Iron Lung #{0}] You crashed at ({1},{2})! A strike will be issued and your position will be reset.", moduleId, XPos.ToString("0.00"), YPos.ToString("0.00")); StartCoroutine(Crash()); } //OOPS! WALL IN THE WAY! Also this resets the module.
        if (XPos >= 1000 || XPos <= 0 || YPos >= 1000 || YPos <= 0){ Debug.LogFormat("[Iron Lung #{0}] You left the navigation area! A strike will be issued and your position will be reset.", moduleId); StartCoroutine(Crash()); } //This also resets, but doing this is probably even less likely to happen on a legitimate attempt. Good job.
    }

    IEnumerator GoalFinding()
    {
        bool Cool = true; //no idea where this name came from tbh
        while (Cool)
        {
            yield return new WaitForSeconds(0.2f); //Eh, don't know why I added this delay. Guess I thought it helped with logging everything correctly
            GoalPosition[0] = UnityEngine.Random.Range(0, 1000);
            GoalPosition[1] = UnityEngine.Random.Range(0, 1000);
            GoalPosition[2] = UnityEngine.Random.Range(0, 360);
            Debug.LogFormat("[Iron Lung #{0}] Chosen goal position is ({1},{2}), at an angle of {3}.", moduleId, GoalPosition[0], GoalPosition[1], GoalPosition[2]);
            if (GoalPosition[0] > 300 && GoalPosition[0] < 700 && GoalPosition[1] > 300 && GoalPosition[1] < 700) Debug.LogFormat("[Iron Lung #{0}] That position is too close to the center. Retrying...", moduleId); //Pretty much every location in the center is easy to reach, so there's no challenge whatsoever
            else if (MapInUse[99 - (GoalPosition[1] / 10)].Substring((GoalPosition[0] / 10), 1) == "0") Cool = false;
            else Debug.LogFormat("[Iron Lung #{0}] That position is within a wall. Retrying...", moduleId); //Would be literally impossible without this
        }
        Audio.PlaySoundAtTransform("click", transform);
        if (GoalPosition[0] < 10) Goal[0].text = "X_00" + GoalPosition[0].ToString();
        else if (GoalPosition[0] < 100) Goal[0].text = "X_0" + GoalPosition[0].ToString();
        else Goal[0].text = "X_" + GoalPosition[0].ToString();
        yield return new WaitForSecondsRealtime(0.4f);
        Audio.PlaySoundAtTransform("click", transform);
        if (GoalPosition[1] < 10) Goal[1].text = "Y_00" + GoalPosition[1].ToString();
        else if (GoalPosition[1] < 100) Goal[1].text = "Y_0" + GoalPosition[1].ToString();
        else Goal[1].text = "Y_" + GoalPosition[1].ToString();
        yield return new WaitForSecondsRealtime(0.4f);
        Audio.PlaySoundAtTransform("click", transform);
        if (GoalPosition[2] < 10) Goal[2].text = "A_00" + GoalPosition[2].ToString();
        else if (GoalPosition[2] < 100) Goal[2].text = "A_0" + GoalPosition[2].ToString();
        else Goal[2].text = "A_" + GoalPosition[2].ToString();
        yield return new WaitForSecondsRealtime(0.4f);
    }
    //THIS IS NOT THE BEST WAY TO ADD ALL THESE PROXIMITY SENSOR CHECKS BUT I TRIED MY BEST, SORRY
    IEnumerator ProximityCheckNorth() 
    {
        if (YPos < 990)
        {
            if (MapInUse[98 - ((int)YPos / 10)].Substring(((int)XPos / 10), 1) == "1") //Sure, this works
            {
                if (!ActiveProxSensors[0]) Debug.LogFormat("[Iron Lung #{0}] Northern proximity sensor triggered! Position at time of activation is ({1},{2}).", moduleId, XPos.ToString("0.00"), YPos.ToString("0.00")); //Helps to prove they still work in a situation like ramming into a corner
                ActiveProxSensors[0] = true;
                Proximity[0].SetActive(true);
                Audio.PlaySoundAtTransform("proximity", transform);
                yield return new WaitForSeconds(0.5f - (((YPos % 10) + 10) / 40)); //Also this is to make it flash faster and faster the closer you are to the wall
                Proximity[0].SetActive(false);
                yield return new WaitForSeconds(0.5f - (((YPos % 10) + 10) / 40));
            }
            else if (YPos < 980) //Someone requested making the limit be 20 units before triggering, and that seemed fair to me so I did that
            {
                if (MapInUse[97 - ((int)YPos / 10)].Substring(((int)XPos / 10), 1) == "1")
                {
                    if (!ActiveProxSensors[0]) Debug.LogFormat("[Iron Lung #{0}] Northern proximity sensor triggered! Position at time of activation is ({1},{2}).", moduleId, XPos.ToString("0.00"), YPos.ToString("0.00"));
                    ActiveProxSensors[0] = true;
                    Proximity[0].SetActive(true);
                    Audio.PlaySoundAtTransform("proximity", transform);
                    yield return new WaitForSeconds(0.5f - ((YPos % 10) / 40));
                    Proximity[0].SetActive(false);
                    yield return new WaitForSeconds(0.5f - ((YPos % 10) / 40));
                }
                else { ActiveProxSensors[0] = false; yield return new WaitForSeconds(0.05f); }
        }

            else { ActiveProxSensors[0] = false; yield return new WaitForSeconds(0.05f); }
        }
        else yield return new WaitForSeconds(0.05f);
        StartCoroutine(ProximityCheckNorth()); //The checks happen constantly because there's no point stopping it, makes them worse than useless
    }

    IEnumerator ProximityCheckEast() //Also everything above applies to all these other prox sensors as well
    {
        if (XPos < 990)
        {
            if (MapInUse[99 - ((int)YPos / 10)].Substring(((int)XPos / 10) + 1, 1) == "1")
            {
                if (!ActiveProxSensors[1]) Debug.LogFormat("[Iron Lung #{0}] Eastern proximity sensor triggered! Position at time of activation is ({1},{2}).", moduleId, XPos.ToString("0.00"), YPos.ToString("0.00"));
                ActiveProxSensors[1] = true;
                Proximity[1].SetActive(true);
                Audio.PlaySoundAtTransform("proximity", transform);
                yield return new WaitForSeconds(0.5f - (((XPos % 10) + 10) / 40));
                Proximity[1].SetActive(false);
                yield return new WaitForSeconds(0.5f - (((XPos % 10) + 10) / 40));
            }
            else if (XPos < 980)
            {
                if (MapInUse[99 - ((int)YPos / 10)].Substring(((int)XPos / 10) + 2, 1) == "1")
                {
                    if (!ActiveProxSensors[1]) Debug.LogFormat("[Iron Lung #{0}] Eastern proximity sensor triggered! Position at time of activation is ({1},{2}).", moduleId, XPos.ToString("0.00"), YPos.ToString("0.00"));
                    ActiveProxSensors[1] = true;
                    Proximity[1].SetActive(true);
                    Audio.PlaySoundAtTransform("proximity", transform);
                    yield return new WaitForSeconds(0.5f - ((XPos % 10) / 40));
                    Proximity[1].SetActive(false);
                    yield return new WaitForSeconds(0.5f - ((XPos % 10) / 40));
                }
                else { ActiveProxSensors[1] = false; yield return new WaitForSeconds(0.05f); }
            }
            else { ActiveProxSensors[1] = false; yield return new WaitForSeconds(0.05f); }
        }
        else yield return new WaitForSeconds(0.05f);
        StartCoroutine(ProximityCheckEast());
    }

    IEnumerator ProximityCheckSouth()
    {
        if (YPos >= 10)
        {
            if (MapInUse[100 - ((int)YPos / 10)].Substring(((int)XPos / 10), 1) == "1")
            {
                if (!ActiveProxSensors[2]) Debug.LogFormat("[Iron Lung #{0}] Southern proximity sensor triggered! Position at time of activation is ({1},{2}).", moduleId, XPos.ToString("0.00"), YPos.ToString("0.00"));
                ActiveProxSensors[2] = true;
                Proximity[2].SetActive(true);
                Audio.PlaySoundAtTransform("proximity", transform);
                yield return new WaitForSeconds((YPos % 10) / 40);
                Proximity[2].SetActive(false);
                yield return new WaitForSeconds((YPos % 10) / 40);
            }
            else if (YPos >= 20)
            {
                if (MapInUse[101 - ((int)YPos / 10)].Substring(((int)XPos / 10), 1) == "1")
                {
                    if (!ActiveProxSensors[2]) Debug.LogFormat("[Iron Lung #{0}] Southern proximity sensor triggered! Position at time of activation is ({1},{2}).", moduleId, XPos.ToString("0.00"), YPos.ToString("0.00"));
                    ActiveProxSensors[2] = true;
                    Proximity[2].SetActive(true);
                    Audio.PlaySoundAtTransform("proximity", transform);
                    yield return new WaitForSeconds(((YPos % 10) + 10) / 40);
                    Proximity[2].SetActive(false);
                    yield return new WaitForSeconds(((YPos % 10) + 10) / 40);
                }
                else { ActiveProxSensors[2] = false; yield return new WaitForSeconds(0.05f); }
            }
            else { ActiveProxSensors[2] = false; yield return new WaitForSeconds(0.05f); }
        }
        else yield return new WaitForSeconds(0.05f);
        StartCoroutine(ProximityCheckSouth());
    }

    IEnumerator ProximityCheckWest()
    {
        if (XPos >= 10)
        {
            if (MapInUse[99 - ((int)YPos / 10)].Substring(((int)XPos / 10) - 1, 1) == "1")
            {
                if (!ActiveProxSensors[3]) Debug.LogFormat("[Iron Lung #{0}] Western proximity sensor triggered! Position at time of activation is ({1},{2}).", moduleId, XPos.ToString("0.00"), YPos.ToString("0.00"));
                ActiveProxSensors[3] = true;
                Proximity[3].SetActive(true);
                Audio.PlaySoundAtTransform("proximity", transform);
                yield return new WaitForSeconds((XPos % 10) / 40);
                Proximity[3].SetActive(false);
                yield return new WaitForSeconds((XPos % 10) / 40);
            }
            else if (XPos >= 20)
            {
                if (MapInUse[99 - ((int)YPos / 10)].Substring(((int)XPos / 10) - 2, 1) == "1")
                {
                    if (!ActiveProxSensors[3]) Debug.LogFormat("[Iron Lung #{0}] Western proximity sensor triggered! Position at time of activation is ({1},{2}).", moduleId, XPos.ToString("0.00"), YPos.ToString("0.00"));
                    ActiveProxSensors[3] = true;
                    Proximity[3].SetActive(true);
                    Audio.PlaySoundAtTransform("proximity", transform);
                    yield return new WaitForSeconds(((XPos % 10) + 10) / 40);
                    Proximity[3].SetActive(false);
                    yield return new WaitForSeconds(((XPos % 10) + 10) / 40);
                }
                else { ActiveProxSensors[3] = false; yield return new WaitForSeconds(0.05f); }
            }
            else { ActiveProxSensors[3] = false; yield return new WaitForSeconds(0.05f); }
        }
        else yield return new WaitForSeconds(0.05f);
        StartCoroutine(ProximityCheckWest());
    }

    IEnumerator Crash() //Y'know this is normally rather hard to do unless you're being silly so, uhhhhh... congrats?
    {
        Module.HandleStrike();
        ModuleSolved = true;
        Blackout.SetActive(true); //Oh hey this also allows all the resetting to be hidden behind a silly little black square!
        Audio.PlaySoundAtTransform("crash", transform);
        PreventingButtonsCommittingFloatation = true;
        if (BackwardHeld)
        {
            Down[0].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
            Down[1].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
            BackwardHeld = false;
        }
        if (ForwardHeld)
        {
            Up[0].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
            Up[1].transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
            ForwardHeld = false;
        }
        if (LeftTurnButtonHeld)
        {
            CCW[0].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
            CCW[1].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
            LeftTurnButtonHeld = false;
        }
        if (RightTurnButtonHeld)
        {
            CW[0].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
            CW[1].transform.localPosition += new Vector3(0.0f, -0.5f, 0.0f);
            RightTurnButtonHeld = false;
        }
        RotationCalc = 0;
        SpeedCalc = 0;
        XPos = 500.00f;
        YPos = 500.00f;
        DirectionArrow.transform.Rotate(0.0f, 0.0f, Angle * -1f, Space.Self); //Resets it to exactly north, way easier than I expected tbh
        Angle = 0.00f;
        yield return new WaitForSecondsRealtime(5f);
        ModuleSolved = false;
        Blackout.SetActive(false);
    }

    IEnumerator Oxygen() //Because why not add a silly little thing for if you're being S L O W
    {
        yield return new WaitForSecondsRealtime(1200f); //Why 20 minutes? idk
        while (true)
        {
            if (ModuleSolved) break;
            int random = UnityEngine.Random.Range(0, 100); //Because why not make it unpredictable
            if (random == 0)
            {
                OxygenCounter++;
                Audio.PlaySoundAtTransform("oxygen", transform);
                break;
            }
            else
            {
                yield return new WaitForSecondsRealtime(2.5f);
            }
        }
        if (OxygenCounter != 3)
            StartCoroutine(Oxygen());
    }

    //twitch plays
    bool TwitchShouldCancelCommand; //Also THANK EXISH FOR ALL OF THIS I STILL HAVE NO IDEA HOW HE PULLED IT OFF BUT WOW
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} angle <###> [Sets the angle of the submarine] | !{0} forward/backward <x/y> <###> [Moves the submarine forward or backward until the x or y of your current position is '###' or a new proximity sensor goes off] | !{0} submit [Submits your current position]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (ModuleSolved)
            {
                yield return "sendtochaterror Submit cannot be pressed while the module is animating!";
                yield break;
            }
            yield return null;
            Photo.OnInteract();
            if (SolveIncoming)
                yield return "solve";
            else
                yield return "strike";
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*angle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify an angle for the submarine!";
            else if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else
            {
                int temp = -1;
                if (!int.TryParse(parameters[1], out temp))
                {
                    yield return "sendtochaterror!f The specified angle '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (temp < 0 || temp > 359)
                {
                    yield return "sendtochaterror The specified angle '" + parameters[1] + "' is out of range 0-359!";
                    yield break;
                }
                if (ModuleSolved)
                {
                    yield return "sendtochaterror The angle cannot be set while the module is animating!";
                    yield break;
                }
                yield return null;
                calc:
                int ct1 = 0, ct2 = 0;
                int start = (int)Angle;
                while (start != temp)
                {
                    ct1++;
                    start++;
                    if (start > 359)
                        start = 0;
                }
                start = (int)Angle;
                while (start != temp)
                {
                    ct2++;
                    start--;
                    if (start < 0)
                        start = 359;
                }
                if (ct1 < ct2)
                {
                    if (PreventingButtonsCommittingFloatation) Direction[0].OnInteractEnded();
                    Direction[0].OnInteract();
                    while ((int)Angle != temp)
                    {
                        yield return null;
                        if (TwitchShouldCancelCommand)
                            break;
                    }
                    Direction[0].OnInteractEnded();
                }
                else
                {
                    if (PreventingButtonsCommittingFloatation) Direction[1].OnInteractEnded();
                    Direction[1].OnInteract();
                    while ((int)Angle != temp)
                    {
                        yield return null;
                        if (TwitchShouldCancelCommand)
                            break;
                    }
                    Direction[1].OnInteractEnded();
                }
                if (!TwitchShouldCancelCommand)
                {
                    while (RotationCalc != 0f)
                    {
                        yield return null;
                        if (TwitchShouldCancelCommand)
                            break;
                    }
                    if (!TwitchShouldCancelCommand)
                    {
                        if ((int)Angle != temp)
                            goto calc;
                    }
                    else
                        yield return "cancelled";
                }
                else
                    yield return "cancelled";
            }
            yield break;
        }
        if (Regex.IsMatch(parameters[0], @"^\s*forward|backward\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify a value and whether x or y should be that value!";
            else if (parameters.Length == 2 && (parameters[1].EqualsIgnoreCase("x") || parameters[1].EqualsIgnoreCase("y")))
                yield return "sendtochaterror Please specify a value that " + parameters[1] + " should be!";
            else if (parameters.Length == 2)
                yield return "sendtochaterror!f Expected x or y but received '" + parameters[1] + "'!";
            else if (parameters.Length > 3)
                yield return "sendtochaterror Too many parameters!";
            else
            {
                if (!parameters[1].EqualsIgnoreCase("x") && !parameters[1].EqualsIgnoreCase("y"))
                {
                    yield return "sendtochaterror!f Expected x or y but received '" + parameters[1] + "'!";
                    yield break;
                }
                int temp = -1;
                if (!int.TryParse(parameters[2], out temp))
                {
                    yield return "sendtochaterror!f The specified value '" + parameters[2] + "' is invalid!";
                    yield break;
                }
                if (temp < 0 || temp > 999)
                {
                    yield return "sendtochaterror The specified value '" + parameters[2] + "' is out of range 0-999!";
                    yield break;
                }
                if (ModuleSolved)
                {
                    yield return "sendtochaterror The submarine cannot be moved forward or backward while the module is animating!";
                    yield break;
                }
                yield return null;
                bool[] activeProx = new bool[4];
                for (int i = 0; i < 4; i++)
                    activeProx[i] = ActiveProxSensors[i];
                bool goForward = parameters[0].EqualsIgnoreCase("forward");
            calc:
                bool leave = false;
                if (PreventingButtonsCommittingFloatation) Movement[goForward ? 1 : 0].OnInteractEnded();
                Movement[goForward ? 1 : 0].OnInteract();
                if (parameters[1].EqualsIgnoreCase("x"))
                {
                    while ((int)XPos != temp)
                    {
                        yield return null;
                        for (int i = 0; i < 4; i++)
                        {
                            if (activeProx[i] == false && ActiveProxSensors[i] == true)
                            {
                                leave = true;
                                break;
                            }
                        }
                        if (TwitchShouldCancelCommand || leave)
                            break;
                    }
                }
                else
                {
                    while ((int)YPos != temp)
                    {
                        yield return null;
                        for (int i = 0; i < 4; i++)
                        {
                            if (activeProx[i] == false && ActiveProxSensors[i] == true)
                            {
                                leave = true;
                                break;
                            }
                        }
                        if (TwitchShouldCancelCommand || leave)
                            break;
                    }
                }
                Movement[goForward ? 1 : 0].OnInteractEnded();
                if (!TwitchShouldCancelCommand && !leave)
                {
                    while (SpeedCalc != 0f)
                    {
                        yield return null;
                        if (TwitchShouldCancelCommand)
                            break;
                    }
                    if (!TwitchShouldCancelCommand)
                    {
                        if ((parameters[1].EqualsIgnoreCase("x") && temp != (int)XPos) || (parameters[1].EqualsIgnoreCase("y") && temp != (int)YPos))
                        {
                            goForward = !goForward;
                            goto calc;
                        }
                    }
                    else
                        yield return "cancelled";
                }
                if (TwitchShouldCancelCommand)
                    yield return "cancelled";
            }
        }
    }
}