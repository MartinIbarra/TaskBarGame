using System;

namespace TaskbarTactics.Core.Models
{
    [Serializable]
    public struct FormationPosition : IEquatable<FormationPosition>
    {
        public int Row;
        public int Column;

        public FormationPosition(int row, int column)
        {
            if (row < 0 || row > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            if (column < 0 || column > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }

            Row = row;
            Column = column;
        }

        public int ManhattanDistance(FormationPosition other)
        {
            return Math.Abs(Row - other.Row) + Math.Abs(Column - other.Column);
        }

        public bool Equals(FormationPosition other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is FormationPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Row * 397) ^ Column;
            }
        }

        public override string ToString()
        {
            return $"({Row},{Column})";
        }
    }
}
