using FluentAssertions;

namespace BareMediator.Tests;

public class UnitTests
{
    [Fact]
    public void Value_ShouldBeDefaultInstance()
    {
        // Act
        Unit value = Unit.Value;

        // Assert
        value.Should().Be(default);
    }

    [Fact]
    public void Equals_WithAnotherUnit_ShouldReturnTrue()
    {
        // Arrange
        Unit unit1 = Unit.Value;
        Unit unit2 = new();

        // Act
        bool result = unit1.Equals(unit2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithObject_ShouldReturnTrue()
    {
        // Arrange
        Unit unit = Unit.Value;
        object other = new Unit();

        // Act
        bool result = unit.Equals(other);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNonUnitObject_ShouldReturnFalse()
    {
        // Arrange
        Unit unit = Unit.Value;
        object other = "not a unit";

        // Act
        bool result = unit.Equals(other);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        Unit unit = Unit.Value;

        // Act
        bool result = unit.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_ShouldReturnTrue()
    {
        // Arrange
        Unit unit1 = Unit.Value;
        Unit unit2 = new();

        // Act
        bool result = unit1 == unit2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_ShouldReturnFalse()
    {
        // Arrange
        Unit unit1 = Unit.Value;
        Unit unit2 = new();

        // Act
        bool result = unit1 != unit2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldAlwaysReturnZero()
    {
        // Arrange
        Unit unit1 = Unit.Value;
        Unit unit2 = new();

        // Act
        int hash1 = unit1.GetHashCode();
        int hash2 = unit2.GetHashCode();

        // Assert
        hash1.Should().Be(0);
        hash2.Should().Be(0);
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void CompareTo_ShouldAlwaysReturnZero()
    {
        // Arrange
        Unit unit1 = Unit.Value;
        Unit unit2 = new();

        // Act
        int result = unit1.CompareTo(unit2);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void ToString_ShouldReturnEmptyParentheses()
    {
        // Arrange
        Unit unit = Unit.Value;

        // Act
        string result = unit.ToString();

        // Assert
        result.Should().Be("()");
    }

    [Fact]
    public void MultipleInstances_ShouldAllBeEqual()
    {
        // Arrange
        Unit[] units = [Unit.Value, new Unit(), default];

        // Act & Assert
        for (int i = 0; i < units.Length; i++)
        {
            for (int j = 0; j < units.Length; j++)
            {
                units[i].Should().Be(units[j]);
                (units[i] == units[j]).Should().BeTrue();
                units[i].Equals(units[j]).Should().BeTrue();
            }
        }
    }
}
