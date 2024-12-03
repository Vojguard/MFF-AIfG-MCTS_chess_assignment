using System.Collections.Generic;

namespace Chess
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class MCTSNode
    {
        public MCTSNode Parent { get; private set; }
        public List<MCTSNode> Children { get; private set; }
        public object GameState { get; private set; }
        public int Visits { get; private set; }
        public float Score { get; private set; }
        public bool IsFullyExpanded => Children.Count > 0 && Children.TrueForAll(child => child.Visits > 0);

        public MCTSNode(object gameState, MCTSNode parent = null)
        {
            GameState = gameState;
            Parent = parent;
            Children = new List<MCTSNode>();
            Visits = 0;
            Score = 0;
        }

        public void AddChild(MCTSNode child)
        {
            Children.Add(child);
        }

        public void UpdateStats(float result)
        {
            Visits++;
            Score += result;
        }

        public MCTSNode GetBestChild(float explorationParameter)
        {
            MCTSNode bestChild = null;
            float bestValue = float.MinValue;

            foreach (var child in Children)
            {
                float uctValue = (child.Score / child.Visits) +
                                 explorationParameter * Mathf.Sqrt(Mathf.Log(Visits + 1) / (child.Visits));

                if (uctValue > bestValue)
                {
                    bestValue = uctValue;
                    bestChild = child;
                }
            }

            return bestChild;
        }
    }
}