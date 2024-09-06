﻿using MajdataPlay.Extensions;
using MajdataPlay.Interfaces;
using MajdataPlay.IO;
using MajdataPlay.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MajdataPlay.Game.Notes
{
    public abstract class SlideBase : NoteLongDrop
    {
        public ConnSlideInfo ConnectInfo { get; set; } = new()
        {
            IsGroupPart = false,
            Parent = null
        };
        /// <summary>
        /// 如果判定队列已经完成，返回True，反之False
        /// </summary>
        public bool IsFinished { get => QueueRemaining == 0; }
        /// <summary>
        /// 如果判定队列剩余1个未完成判定区，返回True
        /// </summary>
        public bool IsPendingFinish { get => QueueRemaining == 1; }
        /// <summary>
        /// 返回判定队列中未完成判定区的数量
        /// </summary>
        public int QueueRemaining 
        { 
            get
            {
                int[] reamaining = new int[3];
                foreach (var (i, queue) in judgeQueues.WithIndex())
                    reamaining[i] = queue.Length;
                return reamaining.Max();
            }
        }

        protected JudgeArea[][] judgeQueues = new JudgeArea[3][]
        { 
            Array.Empty<JudgeArea>(), 
            Array.Empty<JudgeArea>(), 
            Array.Empty<JudgeArea>()
        }; // 判定队列

        protected GameObject[] slideBars = { }; // Arrows


        /// <summary>
        /// a timing of slide start
        /// </summary>
        public float timeStart;
        public int sortIndex;
        public bool isJustR;
        public float fadeInTiming;
        public float fullFadeInTiming;
        public int endPosition;
        public string slideType;

        /// <summary>
        /// 引导Star
        /// </summary>
        public GameObject[] stars = new GameObject[3];

        protected Animator fadeInAnimator;
        protected GameObject slideOK;
        protected bool isSoundPlayed = false;
        protected float lastWaitTime;
        protected bool canCheck = false;
        protected bool isChecking = false;
        
        protected bool isInitialized = false; //防止重复初始化
        protected bool isDestroying = false; // 防止重复销毁
        /// <summary>
        /// 存储Slide Queue中会经过的区域
        /// <para>用于绑定或解绑Event</para>
        /// </summary>
        protected IEnumerable<SensorType> judgeAreas;
        public abstract void Initialize();
        protected void Judge()
        {
            if (!ConnectInfo.IsGroupPartEnd && ConnectInfo.IsConnSlide)
                return;
            else if (isJudged)
                return;
            //var stayTime = time + LastFor - judgeTiming; // 停留时间
            var stayTime = lastWaitTime; // 停留时间

            // By Minepig
            var diff = GetTimeSpanToJudgeTiming();
            var isFast = diff < 0;

            // input latency simulation
            //var ext = MathF.Max(0.05f, MathF.Min(stayTime / 4, 0.36666667f));
            var ext = MathF.Min(stayTime / 4, 0.36666667f);

            var perfect = 0.2333333f + ext;

            diff = MathF.Abs(diff);
            JudgeType? judge = null;

            if (diff <= perfect)// 其实最小0.2833333f, 17帧
                judge = JudgeType.Perfect;
            else
            {
                judge = diff switch
                {
                    <= 0.35f => isFast ? JudgeType.FastGreat : JudgeType.LateGreat,
                    <= 0.4166667f => isFast ? JudgeType.FastGreat1 : JudgeType.LateGreat1,
                    <= 0.4833333f => isFast ? JudgeType.FastGreat2 : JudgeType.LateGreat2,
                    _ => isFast ? JudgeType.FastGood : JudgeType.LateGood
                };
            }

            print($"Slide diff : {MathF.Round(diff * 1000, 2)} ms");
            judgeResult = judge ?? JudgeType.Miss;
            isJudged = true;

            if (GetTimeSpanToArriveTiming() < 0)
                lastWaitTime = MathF.Abs(GetTimeSpanToArriveTiming()) / 2;
            else if (diff >= 0.6166679 && !isFast)
                lastWaitTime = 0;
        }
        protected void Judge_Classic()
        {
            if (!ConnectInfo.IsGroupPartEnd && ConnectInfo.IsConnSlide)
                return;
            else if (isJudged)
                return;

            var diff = GetTimeSpanToJudgeTiming();
            var isFast = diff < 0;

            var perfect = 0.15f;

            diff = MathF.Abs(diff);
            JudgeType? judge = null;

            if (diff <= perfect)
                judge = JudgeType.Perfect;
            else
            {
                judge = diff switch
                {
                    <= 0.2305557f => isFast ? JudgeType.FastGreat : JudgeType.LateGreat,
                    <= 0.3111114f => isFast ? JudgeType.FastGreat1 : JudgeType.LateGreat1,
                    <= 0.3916672f => isFast ? JudgeType.FastGreat2 : JudgeType.LateGreat2,
                    _ => isFast ? JudgeType.FastGood : JudgeType.LateGood
                };
            }

            print($"Slide diff : {MathF.Round(diff * 1000, 2)} ms");
            judgeResult = judge ?? JudgeType.Miss;
            isJudged = true;

            if (GetTimeSpanToArriveTiming() < 0)
                lastWaitTime = MathF.Abs(GetTimeSpanToArriveTiming()) / 2;
            else if (diff >= 0.6166679 && !isFast)
                lastWaitTime = 0;
        }
        protected void HideBar(int endIndex)
        {
            endIndex = endIndex - 1;
            endIndex = Math.Min(endIndex, slideBars.Length - 1);
            for (int i = 0; i <= endIndex; i++)
                slideBars[i].SetActive(false);
        }
        protected void PlaySlideOK()
        {
            if (slideOK == null)
                return;
            var canPlay = CheckSetting();

            if (canPlay)
                slideOK.SetActive(true);
            else
                Destroy(slideOK);
        }
        bool CheckSetting()
        {
            var slideSetting = GameManager.Instance.Setting.Display.SlideJudgeType;
            var resultValue = (int)judgeResult;
            var absValue = Math.Abs(7 - resultValue);

            return slideSetting switch
            {
                JudgeDisplayType.All => true,
                JudgeDisplayType.BelowCP => resultValue != 7,
                JudgeDisplayType.BelowP => absValue > 2,
                JudgeDisplayType.BelowGR => absValue > 5,
                JudgeDisplayType.All_BreakOnly => isBreak,
                JudgeDisplayType.BelowCP_BreakOnly => absValue != 0 && isBreak,
                JudgeDisplayType.BelowP_BreakOnly => absValue > 2 && isBreak,
                JudgeDisplayType.BelowGR_BreakOnly => absValue > 5 && isBreak,
                _ => false
            };
        }
        protected void HideAllBar() => HideBar(int.MaxValue);
        protected void SetSlideBarAlpha(float alpha)
        {
            foreach (var gm in slideBars)
                gm.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, alpha);
        }
        protected void TooLateJudge()
        {
            if (isJudged)
            {
                DestroySelf();
                return;
            }

            if (QueueRemaining == 1)
                judgeResult = JudgeType.LateGood;
            else
                judgeResult = JudgeType.Miss;
            isJudged = true;
            DestroySelf();
        }
        /// <summary>
        /// 销毁当前Slide
        /// <para>当 <paramref name="onlyStar"/> 为true时，仅销毁引导Star</para>
        /// </summary>
        /// <param name="onlyStar"></param>
        protected void DestroySelf(bool onlyStar = false)
        {

            if (onlyStar)
                DestroyStars();
            else
            {
                if (ConnectInfo.Parent != null)
                    Destroy(ConnectInfo.Parent);

                foreach (GameObject obj in slideBars)
                    obj.SetActive(false);

                DestroyStars();
                Destroy(gameObject);
            }
        }
        /// <summary>
        /// Connection Slide
        /// <para>强制完成该Slide</para>
        /// </summary>
        protected void ForceFinish()
        {
            if (!ConnectInfo.IsConnSlide || ConnectInfo.IsGroupPartEnd)
                return;
            HideAllBar();
            var emptyQueue = Array.Empty<JudgeArea>();
            for (int i = 0; i < 2; i++)
                judgeQueues[i] = emptyQueue;
        }
        void DestroyStars()
        {
            if (stars.IsEmpty())
                return;
            foreach (var star in stars)
            {
                if (star != null)
                    Destroy(star);
            }
            stars = Array.Empty<GameObject>();
        }
    }
}
