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
        enum KingDead
        {
            None,
            Black,
            White,
        }

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

            rootSearchNode = new(board, Move.InvalidMove);
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
            ExpandNode(rootSearchNode, true);
            searchStopwatch.Start();
            int numOfPlayouts = 0;
            while (searchStopwatch.ElapsedMilliseconds < settings.searchTimeMillis && numOfPlayouts < settings.maxNumOfPlayouts && !abortSearch)
            {
                MCTSNode selectedNode = SelectBestNode();
                if (selectedNode.Visits > 0)
                {
                    selectedNode = ExpandNode(selectedNode, false);
                }
                float simulationResult = Simulate(selectedNode);
                Backpropagate(selectedNode,1 - simulationResult);

                numOfPlayouts++;

                bestMove = rootSearchNode.Children
                .OrderByDescending(child => child.Score)
                .First().Move;
            }
            searchStopwatch.Stop();
            
            foreach (var child in rootSearchNode.Children)
            {
                //Debug.Log(child.Move.Name + " " + child.Visits + " " + child.Score);
            }
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

        MCTSNode ExpandNode(MCTSNode nodeToExpand, bool isRoot)
        {
            var possibleMoves = moveGenerator.GenerateMoves(nodeToExpand.Board, isRoot);
            possibleMoves.Reverse();
            MCTSNode node = nodeToExpand;
            foreach (var move in possibleMoves)
            {
                var boardCopy = nodeToExpand.Board.Clone();
                boardCopy.MakeMove(move);
                node = new(boardCopy, move, nodeToExpand);
                nodeToExpand.AddChild(node);
            }
            return node;
        }

        float Simulate(MCTSNode nodeForSim)
        {
            var simBoard = nodeForSim.Board.GetLightweightClone();
            bool whiteTurn = nodeForSim.Board.WhiteToMove;

            int playoutDepth = 0;
            while (playoutDepth < settings.playoutDepthLimit && GetKingCaptured(simBoard) == KingDead.None)
            {
                var possibleMoves = moveGenerator.GetSimMoves(simBoard, whiteTurn);
                SimMove randomMove = possibleMoves[rand.Next(possibleMoves.Count)];

                MoveSimPiece(simBoard, randomMove);
                whiteTurn = !whiteTurn;
                playoutDepth++;
            }

            KingDead res = GetKingCaptured(simBoard);
            if (res == KingDead.None)
            {
                return evaluation.EvaluateSimBoard(simBoard, nodeForSim.Board.WhiteToMove);
            }
            else
            {
                return EvalSimEnd(res, nodeForSim.Board.WhiteToMove);
            }

        }

        void Backpropagate(MCTSNode simNode, float result)
        {
            while (simNode != null)
            {
                simNode.UpdateStats(result);
                result = 1 - result;
                simNode = simNode.Parent;
            }
        }

        KingDead GetKingCaptured(SimPiece[,] simState)
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
                    if (whiteAlive && blackAlive) return KingDead.None;
                }
            }

            if (!blackAlive)
            {
                return KingDead.Black;
            }
            else if (!whiteAlive)
            {
                return KingDead.White;
            }
            else
            {
                return KingDead.None;
            }
        }

        float EvalSimEnd(KingDead deadKing, bool whiteToMove)
        {
            int res = -1;
            if (whiteToMove)
            {
                if (deadKing == KingDead.Black)
                {
                    res = 1;
                } else
                {
                    res = 0;
                }
            }
            else
            {
                if (deadKing == KingDead.White)
                {
                    res = 1;
                }
                else
                {
                    res = 0;
                }
            }
            //Debug.Log(deadKing.ToString() + " & " + whiteToMove + " = " + res);
            return res;
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