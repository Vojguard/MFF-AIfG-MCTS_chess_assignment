namespace Chess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using UnityEngine;
    using static System.Math;
    class MCTSSearch : ISearch
    {
        public event System.Action<Move> onSearchComplete;

        MoveGenerator moveGenerator;

        //Start my shit
        MCTSNode rootSearchNode;
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
            searchStopwatch.Start();
            while(searchStopwatch.ElapsedMilliseconds < settings.searchTimeMillis) 
            {
                MCTSNode selectedNode = SelectBestNode();
                MCTSNode expandedNode = ExpandNode(selectedNode);
                if (expandedNode == null)
                {
                    continue;
                }
                float simulationResult = Simulate(expandedNode);
                Backpropagate(expandedNode, simulationResult);
            }
            searchStopwatch.Stop();

            bestMove = rootSearchNode.Children
                .OrderByDescending(child => child.Visits)
                .First().Move;
        }

        MCTSNode SelectBestNode()
        {
            var node = rootSearchNode;
            while (node.Children.Count > 0)
            {
                node = node.GetBestChild(1);
            }
            return node;
        }

        MCTSNode ExpandNode(MCTSNode nodeToExpand)
        {
            var possibleMoves = moveGenerator.GenerateMoves(nodeToExpand.Board, nodeToExpand == rootSearchNode);
            if (possibleMoves.Count == 0) return null;
            var boardCopy = board.Clone();
            boardCopy.MakeMove(possibleMoves[^1]);
            MCTSNode node = new(boardCopy, nodeToExpand);
            nodeToExpand.AddChild(node);
            return node;
        }

        float Simulate(MCTSNode nodeForSim)
        {
            var simBoard = nodeForSim.Board.GetLightweightClone();
            bool whiteTurn = nodeForSim.Board.WhiteToMove;

            int playoutDepth = 0;
            while (playoutDepth < settings.playoutDepthLimit && GetKingCaptured(simBoard) == -1)
            {
                var possibleMoves = moveGenerator.GetSimMoves(simBoard, whiteTurn);
                SimMove randomMove = possibleMoves[rand.Next(possibleMoves.Count)];

                MoveSimPiece(simBoard, randomMove);
                whiteTurn = !whiteTurn;
                playoutDepth++;
            }

            int res = GetKingCaptured(simBoard);
            if (res == -1)
            {
                return evaluation.EvaluateSimBoard(simBoard, whiteTurn);
            }
            else
            {
                return EvalSimEnd(res, whiteTurn);
            }

        }

        void Backpropagate(MCTSNode simNode, float result)
        {
            while (simNode != null)
            {
                simNode.UpdateStats(result);
                simNode = simNode.Parent;
            }
        }

        int GetKingCaptured(SimPiece[,] simState)
        {
            bool whiteAlive = false;
            bool blackAlive = false;

            for (int row = 0; row < simState.GetLength(0); row++)
            {
                for (int col = 0; col < simState.GetLength(1); col++)
                {
                    SimPiece piece = simState[row, col];
                    if (piece != null && piece.type == SimPieceType.King)
                    {
                        if (simState[row, col].team)
                        {
                            whiteAlive = true;
                        }
                        else
                        {
                            blackAlive = true;
                        }
                    }
                }
            }

            if (!blackAlive)
            {
                return 0;
            }
            else if (!whiteAlive)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        float EvalSimEnd(int deadKing, bool whiteTurn)
        {
            if (deadKing == 0 && !whiteTurn || deadKing == 1 && whiteTurn) return 1;
            return 0;
        }

        void MoveSimPiece(SimPiece[,] simState, SimMove simMove)
        {
            simState[simMove.endCoord1, simMove.endCoord2] = simState[simMove.startCoord1, simMove.startCoord2];
            simState[simMove.startCoord1, simMove.startCoord2] = null;
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