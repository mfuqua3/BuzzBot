﻿using System;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public interface IEpgpService
    {
        void Ep(string aliasName, int value, string memo, TransactionType type = TransactionType.EpManual);
        void Gp(string aliasName, int value, string memo, TransactionType type = TransactionType.GpManual);
        bool Set(string aliasName, int ep, int gp, string memo = "Manual Value Correction");
        void Gp(EpgpAlias alias, int value, string memo, TransactionType type = TransactionType.GpManual);
        void Ep(EpgpAlias alias, int value, string memo, TransactionType type = TransactionType.EpManual);
        void Gp(EpgpAlias alias, Item item, string memo, int overrideGpValue = -1);
        void DeleteTransaction(Guid TransactionId);
    }
}