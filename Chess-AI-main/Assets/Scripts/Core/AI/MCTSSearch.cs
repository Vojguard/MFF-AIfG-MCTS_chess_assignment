namespace Chess
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

        //Start my shit
        MCTSNode rootSearchNode;
        MCTSNode nodeToExpand;
        MCTSNode expansionNode;
        //End my shit

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
            rand = new System.Random();

            //My shit
            rootSearchNode = new(board);
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

        //Start my shit
        void SearchMoves()
        {
            
        }

        void SelectBestNode()
        {
            var node = rootSearchNode;
            while (node.Children.Count > 0)
            {
                node = node.GetBestChild(1);
            }
            nodeToExpand = node;
        }

        void ExpandNode()
        {
            MCTSNode node = new(nodeToExpand.Board, nodeToExpand);
            nodeToExpand.AddChild(node);
        }

        void Simulate()
        {
            
        }

        void Backpropagate(float result)
        {
            
        }
        //End my shit

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