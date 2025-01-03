﻿namespace Chess
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;
    using static System.Math;
    class MCTSSearch : ISearch
    {
        public event System.Action<Move> onSearchComplete;

        MoveGenerator moveGenerator;

        MCTSNode rootSearchNode;
        MCTSNode nodeToExpand;
        MCTSNode expansionNode;
        Move bestMove;
        int bestEval;
        bool abortSearch;

        MCTSSettings settings;
        Board board;
        Evaluation evaluation;

        System.Random rand;

        // Diagnostics
        public SearchDiagnostics Diagnostics { get; set; }
        System.Diagnostics.Stopwatch searchStopwatch;

        public MCTSSearch(Board board, MCTSSettings settings)
        {
            this.board = board;
            this.settings = settings;
            evaluation = new Evaluation();
            moveGenerator = new MoveGenerator();
            rootSearchNode = new MCTSNode(board);
            rand = new System.Random();
        }

        public void StartSearch()
        {
            InitDebugInfo();

            // Initialize search settings
            bestEval = 0;
            bestMove = Move.InvalidMove;

            moveGenerator.promotionsToGenerate = settings.promotionsToSearch;
            abortSearch = false;
            Diagnostics = new SearchDiagnostics();

            SearchMoves();

            onSearchComplete?.Invoke(bestMove);

            if (!settings.useThreading)
            {
                LogDebugInfo();
            }
        }

        public void EndSearch()
        {
            if (settings.useTimeLimit)
            {
                abortSearch = true;
            }
        }

        void SearchMoves()
        {
            MCTSNode node = rootSearchNode;

            while (node.Children.Count > 0)
            {
                node = node.GetBestChild(1);
                if (abortSearch) break;
            }
            nodeToExpand = node;
        }

        void ExpandNode()
        {
            MCTSNode expandedNode = new MCTSNode(board, nodeToExpand);
            nodeToExpand.Children.Add(expandedNode);
            expansionNode = expandedNode;
        }

        void Simulate()
        {
            List<Move> moves = moveGenerator.GenerateMoves(board, false);
            while (moves.Count > 0)
            {
                board.MakeMove(moves[rand.Next(0, moves.Count - 1)]);
                moves = moveGenerator.GenerateMoves(board, false);
            }
            Backpropagate(evaluation.Evaluate(board));
        }

        void Backpropagate(float result)
        {
            var thisNode = expansionNode;
            while (thisNode != null)
            {
                thisNode.UpdateStats(result);
                thisNode = thisNode.Parent;
            }
        }

        void LogDebugInfo()
        {
            // Optional
        }

        void InitDebugInfo()
        {
            searchStopwatch = System.Diagnostics.Stopwatch.StartNew();
            // Optional
        }
    }
}