﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SoccerGame;
using System;

public class ManualController : MonoBehaviour
{
    [Tooltip("Se o jogador tomar a bola e estiver a uma distancia igual ou menor, a animação de tomada de bola sera executada")]
    public float distanceToEntry = 2.5f;
    public float distanceToDrop = 1.5f;

    private PlayerController player;
    private ControllerLocomotion locomotion { get { return player.Locomotion; } }

    //Seleção manual de jogado para passe de bola
    private PlayerController playerToPass = null;

    private float dir { get { return player.dir; } set { player.dir = value; } }
    private float speed { get { return player.speed; } set { player.speed = value; } }
    private float timeToSelect = 0;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        // SignEvents();

    }
    void Update()
    {
        if (player == null)
            return;

        //Seleciona outro jogador manual mais proximo se eu estiver muito longe da bola
        if (player.GetCampTeam().GetSelectionMode() == GameOptionMode.automatric &&
            player.GetCampTeam().HasPlayerOk() &&
            !player.Locomotion.inHoldTug &&
            !player.Locomotion.isJoint &&
            playerToPass == null)
        {
            timeToSelect += Time.deltaTime;
            if (timeToSelect > 1.5f)
            {
                if (player.Distance(BallController.GetPosition()) > 3.5f || player.isOk == false) //Procurando jogador mais proximo
                {
                    PlayerController nearBall = GameManager.instance.GetPlayerNearBall(player.GetCampTeam());
                    if (nearBall != player)
                    {
                        speed = 0;
                        dir = 0;

                        nearBall.SelectME();
                        timeToSelect = 0.0f;
                        return;
                    }
                }
            }
        }

        //Loockat na bola se estiver em strafe
        if (locomotion.inStrafe)
        {
            locomotion.ResetSpeedMultiples();
            Vector3 ballposition = BallController.GetPosition();
            ballposition.y = transform.position.y;
            transform.LookAt(ballposition);
        }

        //Gerenciamento de stamina
        if (locomotion.inSoccer)
        {
            SkillVar stamina = player.GetSkill_Stamina();
            stamina.mode = SkillVarMode.autoSubtract;
            if (stamina.IsMin || player.isOk == false)
            {
                player.GetSkill_Stamina().mode = SkillVarMode.autoRegen;
                locomotion.ResetSpeedMultiples();
                player.SetMotionNormal();
            }
        }

        //Para ações manuais se estiver tropeçando
        if (player.Locomotion.inStumble)//Tropeçando
        {
            locomotion.ResetSpeedMultiples();
            speed = 0;
            dir = 0;
            return;
        }

        //Solicita avoid dos aliados a frente
        if (player.IsMyBall())
        {
            PlayerController allyBtw = null;
            if (player.IsHitForwad(5.5f, out allyBtw, player.GetCampTeam()))
            {
                if (allyBtw.IsLookAt(player))
                {
                    Vector3 origim = allyBtw.transform.position + (-allyBtw.transform.forward * 4.5f);
                    Vector3 freePos = locomotion.GetRandomNavCircle(origim, 4.5f);
                    allyBtw.GetComponent<AIController>().GoToPosition(freePos, BallController.instance.transform);
                }
            }
        }

        //Se houver um jogador selecionado para o passe de bola vou esperar ficar distante que o jogador efetue alguma ação difernte de se mover
        //para remover a seleção do jogador a passar a bola
        if (playerToPass != null)
        {
            if (playerToPass.Distance(player) > 11.5f)
            {
                playerToPass = null;
                GameManager.instance.ResetIndicator();
            }
        }


        Vector2 move = locomotion.GetDirectionAxis1();
        dir = move.x;
        speed = move.y;


        //Fill kick power amount 
        if (ControllerInput.GetButtonDown(player.GetInputType(), player.GetInputs().Input_Kick))
        {
            //Fill kick power or fill action2 power
            SkillVar skillKick = player.GetSkill_BasicKick();


            if (player.IsMyBall())
            {
                skillKick.SetToggle();

                if (skillKick.IsReady)
                {
                    skillKick.mode = SkillVarMode.autoRegen;
                }
            }

            playerToPass = null;
            GameManager.instance.ResetIndicator();
        }
        //Kick ball
        if (ControllerInput.GetButtonUp(player.GetInputType(), player.GetInputs().Input_Kick))
        {
            //Fill kick power if is myBall
            SkillVar skillKick = player.GetSkill_BasicKick();
            locomotion.ResetHoldTugAnimator();

            if (player.IsMyBall() && skillKick.isToggle)
            {
                if (skillKick.IsReady)
                {
                    if (locomotion.TriggerKick())
                    {
                        skillKick.TriggerCooldown();
                    }
                }
            }

            skillKick.mode = SkillVarMode.nothing;
            skillKick.SetCurrentValue(0);
            skillKick.ResetTogle();

            playerToPass = null;
            GameManager.instance.ResetIndicator();
        }

        //Soccer Motion
        if (ControllerInput.GetButton(player.GetInputType(), player.GetInputs().Input_Stamina))
        {
            SkillVar Stamina = player.GetSkill_Stamina();

            if (Stamina.IsCritical == false && player.isOk)
            {
                // playerToPass = null;
                //  GameManager.instance.ResetIndicator();
                player.SetMotionSoccer();
                player.Locomotion.SetSpeedMultiplies(1.2f);
            }

        }
        if (ControllerInput.GetButtonUp(player.GetInputType(), player.GetInputs().Input_Stamina))
        {
            // playerToPass = null;
            // GameManager.instance.ResetIndicator();
            player.Locomotion.ResetSpeedMultiples();
            player.SetMotionNormal();
            player.GetSkill_Stamina().mode = SkillVarMode.autoRegen;

        }

        //Strafe Motion
        if (ControllerInput.GetButtonDown(player.GetInputType(), player.GetInputs().Input_Strafe))
        {

            // playerToPass = null;
            //  GameManager.instance.ResetIndicator();
            player.SetMotionStrafe();
            player.Locomotion.SetSpeedMultiplies(1.2f);

        }
        if (ControllerInput.GetButtonUp(player.GetInputType(), player.GetInputs().Input_Strafe))
        {
            // playerToPass = null;
            // GameManager.instance.ResetIndicator();
            player.Locomotion.ResetSpeedMultiples();
            player.SetMotionNormal();

        }

        //Seleção para passe de bola
        if (ControllerInput.GetButton(player.GetInputType(), player.GetInputs().Input_Pass))
        {
            if (player.IsMyBall() == true && locomotion.inNormal)
            {

                Vector3 mdirection = transform.forward;
                List<PlayerController> players = player.GetPlayersNear(10.5f);

                if (players.Count > 0)
                {
                    PlayerController target = players.MinAngle(player, mdirection);
                    playerToPass = target;
                    GameManager.instance.IndicatePlayer(playerToPass);
                }
            }
        }
        //Rasteira
        if (ControllerInput.GetButtonDown(player.GetInputType(), player.GetInputs().Input_Pass))
        {
            if (player.IsMyBall() == false && locomotion.inNormal)
            {
                SkillVar skilltrack = player.GetSkill_BasicActionTwo();
                if (skilltrack.IsReady)
                {
                    if (locomotion.TriggerActionTwo())
                    {
                        skilltrack.TriggerCooldown();
                    }
                }

            }
        }

        //Passe de bola
        if (ControllerInput.GetButtonUp(player.GetInputType(), player.GetInputs().Input_Pass))
        {
            //playerToPass = null;
            //GameManager.instance.ResetIndicator();
            SkillVar skillPass = player.GetSkill_BasicPass();
            if (skillPass.IsReady && player.IsMyBall())
            {
                if (locomotion.inNormal)
                {

                    if (locomotion.TriggerPass())
                    {
                        skillPass.TriggerCooldown();
                    }
                    else
                    {
                        playerToPass = null;
                        GameManager.instance.ResetIndicator();
                    }

                }
            }
            else
            {
                playerToPass = null;
                GameManager.instance.ResetIndicator();
            }
        }

        //JUMP
        if (ControllerInput.GetButtonDown(player.GetInputType(), player.GetInputs().Input_Jump))
        {
            locomotion.jump = true;
        }
        if (ControllerInput.GetButtonUp(player.GetInputType(), player.GetInputs().Input_Jump))
        {
            locomotion.jump = false;
        }
    }

    //Unity Events
    private void OnEnable()
    {
        if (player == null)
            player = GetComponent<PlayerController>();

        player.SetMotionNormal();
        SignEvents();
    }
    private void OnDisable()
    {
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Animator>().rootPosition = transform.position;

        UnsignEvents();
    }

    //BallEvents
    private void OnBallSetOwner(PlayerController owner, PlayerController lasOwner)
    {
        //Animação de entrada de bola
        if (owner == player && lasOwner != null)
        {
            float distance = lasOwner.Distance(player);
            if (distance <= distanceToEntry)
            {
                locomotion.TriggerEntry();
            }
        }

        if (playerToPass != null)
        {
            playerToPass = null;
            GameManager.instance.ResetIndicator();
        }
    }
    private void OnBallRemoveOwner(PlayerController lasOwner)
    {

    }

    //Animations Event Tryger
    //Estes eventos são chamados apartir das animações rerentes em quadros espesificos
    private void EvChangeDirectionStart()
    {

        player.SetKinematic();

        if (!player.IsMyBall())
            return;

        BallController.instance.SetBallProtectedTo(player);
        BallController.ChangeDirection();

        //Se o jogador selecionado do time adversario estiver proximo a mim na hora do lésinho, vou fazer ele tropeçar
        CampTeam adversary = player.GetCampTeam() == CampTeam.Team_A ? CampTeam.Team_B : CampTeam.Team_A;

        List<PlayerController> enemys = GameManager.instance.GetPlayersNearBall(adversary, 2.5f);
        if (enemys.Count > 0)
            foreach (PlayerController enemy in enemys)
            {
                //Inimigo que estiver segurando o jogador nao sera afetado
                if (enemy.Locomotion.isJoint == false)
                    enemy.Locomotion.TriggerStumb();
            }

    }
    private void EvChangeDirectionOk()
    {

        // BallController.instance.SetBallDesprotectTo(player);

    }
    private void EvChangeDirectionFinish()
    {
        BallController.instance.SetBallDesprotectTo(player);
        player.UnsetKinematic();
    }

    private void EvTurnDirectionStart()
    {
        player.SetKinematic();

        if (!player.IsMyBall())
            return;

        BallController.instance.SetBallProtectedTo(player);
    }
    private void EvTurnDirectionFinish()
    {
        BallController.instance.SetBallDesprotectTo(player);
        player.UnsetKinematic();
    }

    private void EvPassStart()
    {

        if (player.Locomotion.inAir)
            return;

        if (player.IsMyBall())
            BallController.instance.SetBallProtectedTo(player);

        player.SetKinematic();

    }
    private void EvPassOk()
    {

        if (player.IsMyBall())
        {
            if (playerToPass != null)
            {
                Vector3 dir = playerToPass.transform.position - transform.position;
                Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = rotation;

                playerToPass.GetAiControlelr().WaitPass();
                BallController.SetPass(playerToPass.Distance(player) * 2.0f);
            }
            else
            {

                BallController.SetPass(12.0f);
            }

            // playerToPass = null;
            // GameManager.instance.ResetIndicator();
        }

    }
    private void EvPassFinish()
    {

        player.UnsetKinematic();

        //BUGFIX - previne tompo atrazado
        locomotion.ResetTrip();

        BallController.instance.SetBallDesprotectTo(player);

    }

    private void EvLongKickOk()
    {
        if (BallController.IsOwner(player))
            BallController.SetKick();
    }
    private void EvKickFinish()
    {
        //BUGFIX - previne tompo atrazado
        locomotion.ResetTrip();
    }
    private void EvEntryStart()
    {
        if (BallController.IsOwner(player))
            BallController.instance.SetBallProtectedTo(player);

        player.SetKinematic();

    }
    private void EvEntryFinish()
    {

        BallController.instance.SetBallDesprotectTo(player);
        player.UnsetKinematic();

    }
    private void EvStumbleStart()
    {
        if (player.IsMyBall())
            BallController.SetPass(8.0f);
        player.Locomotion.ResetTriggers();
        player.SetKinematic();
    }
    private void EvStumbleFinish()
    {
        player.UnsetKinematic();
    }

    //Traking
    private void EvTrakStart()
    {
        player.Locomotion.ResetTriggers();
        player.UnsetKinematic();
    }
    private void EvTrakOkt()
    {
        List<PlayerController> enemys = player.GetEnemysNear(distanceToDrop);
        if (enemys.Count > 0)
            foreach (PlayerController enemy in enemys)
            {
                if (enemy.Locomotion.inAir == false)
                    enemy.Locomotion.SetTrip();
            }
    }
    private void EvTrakFinish()
    {
        //BUGIF Previne Trip atrazado
        locomotion.ResetTrip();
    }
    //Triping
    private void EvTripFinish()
    {
        BallController.instance.SetBallDesprotectTo(player);
    }
    private void EvTripStart()
    {
        if (player.IsMyBall())
        {
            BallController.instance.SetBallDesprotectTo(player);
            BallController.SetPass(8.0f);
        }
        player.Locomotion.ResetTriggers();
        player.SetKinematic();
    }
    private void EvStandup()
    {
        player.UnsetKinematic();
    }
    //Private methods
    private void SignEvents()
    {
        PlayerAnimatorEvents animatorEvents = GetComponent<PlayerAnimatorEvents>();
        animatorEvents.OnChangeDirStart += EvChangeDirectionStart;
        animatorEvents.OnChangeDirOk += EvChangeDirectionOk;
        animatorEvents.OnChangeDirFinish += EvChangeDirectionFinish;
        animatorEvents.OnTurnStart += EvTurnDirectionStart;
        animatorEvents.OnTurnFinish += EvTurnDirectionFinish;
        animatorEvents.OnKickOk += EvLongKickOk;
        animatorEvents.OnKickFinish += EvKickFinish;
        animatorEvents.OnPassStart += EvPassStart;
        animatorEvents.OnPassOk += EvPassOk;
        animatorEvents.OnPassFinish += EvPassFinish;
        animatorEvents.OnEnttryStart += EvEntryStart;
        animatorEvents.OnEnttryFinish += EvEntryFinish;
        animatorEvents.OnStumblesStart += EvStumbleStart;
        animatorEvents.OnStumblesFinish += EvStumbleFinish;
        animatorEvents.OnTrackingStart += EvTrakStart;
        animatorEvents.OnTrackingOk += EvTrakOkt;
        animatorEvents.OnTrackingFinish += EvTrakFinish;
        animatorEvents.OnTripingStart += EvTripStart;
        animatorEvents.OnTripingFinish += EvTripFinish;
        animatorEvents.OnOnStandingupFinish += EvStandup;
        StartCoroutine(IESignevents());
    }
    private void UnsignEvents()
    {

        PlayerAnimatorEvents animatorEvents = GetComponent<PlayerAnimatorEvents>();
        animatorEvents.OnChangeDirStart -= EvChangeDirectionStart;
        animatorEvents.OnChangeDirOk -= EvChangeDirectionOk;
        animatorEvents.OnChangeDirFinish -= EvChangeDirectionFinish;
        animatorEvents.OnTurnStart -= EvTurnDirectionStart;
        animatorEvents.OnTurnFinish -= EvTurnDirectionFinish;
        animatorEvents.OnKickOk -= EvLongKickOk;
        animatorEvents.OnKickFinish -= EvKickFinish;
        animatorEvents.OnPassStart -= EvPassStart;
        animatorEvents.OnPassOk -= EvPassOk;
        animatorEvents.OnPassFinish -= EvPassFinish;
        animatorEvents.OnEnttryStart -= EvEntryStart;
        animatorEvents.OnEnttryFinish -= EvEntryFinish;
        animatorEvents.OnStumblesStart -= EvStumbleStart;
        animatorEvents.OnStumblesFinish -= EvStumbleFinish;


        BallController.instance.onSetMyOwner -= OnBallSetOwner;
        BallController.instance.onRemoveMyOwner -= OnBallRemoveOwner;

        animatorEvents.OnTrackingStart -= EvTrakStart;
        animatorEvents.OnTrackingOk -= EvTrakOkt;
        animatorEvents.OnTrackingFinish -= EvTrakFinish;
        animatorEvents.OnTripingStart -= EvTripStart;
        animatorEvents.OnTripingFinish -= EvTripFinish;
        animatorEvents.OnOnStandingupFinish += EvStandup;

    }
    private SkillVar GetSkillsActionTwo()
    {
        SkillVar pass = player.GetSkill_BasicPass();
        SkillVar actionTwo = player.GetSkill_BasicActionTwo();
        SkillVar selectedSkill = player.IsMyBall() ? pass : actionTwo;

        return selectedSkill;
    }
    private SkillVar GetSkillsActioOne()
    {
        SkillVar pass = player.GetSkill_BasicKick();
        SkillVar actionOne = player.GetSkill_BasicActionOne();
        SkillVar selectedSkill = player.IsMyBall() ? pass : actionOne;

        return selectedSkill;
    }
    private IEnumerator IESignevents()
    {

        while (BallController.instance == null)
            yield return null;

        BallController.instance.onSetMyOwner += OnBallSetOwner;
        BallController.instance.onRemoveMyOwner += OnBallRemoveOwner;
    }


}
