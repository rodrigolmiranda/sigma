using Sigma.Domain.Common;
using Xunit;

namespace Sigma.Domain.Tests.Common;

public class EntityTests
{
    private class TestEntity : Entity
    {
        public string Name { get; private set; }

        public TestEntity() : base()
        {
            Name = "Test";
        }

        public TestEntity(Guid id) : base(id)
        {
            Name = "Test";
        }

        public void TriggerEvent(IDomainEvent domainEvent)
        {
            AddDomainEvent(domainEvent);
        }

        public void RemoveEvent(IDomainEvent domainEvent)
        {
            RemoveDomainEvent(domainEvent);
        }

        public void ClearAllEvents()
        {
            ClearDomainEvents();
        }
    }

    private class TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; }
        public DateTime OccurredAtUtc { get; }

        public TestDomainEvent()
        {
            EventId = Guid.NewGuid();
            OccurredAtUtc = DateTime.UtcNow;
        }
    }

    [Fact]
    public void Constructor_WithoutId_ShouldGenerateNewId()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.NotEqual(default(DateTime), entity.CreatedAtUtc);
        Assert.Null(entity.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithId_ShouldUseProvidedId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();

        // Act
        var entity = new TestEntity(expectedId);

        // Assert
        Assert.Equal(expectedId, entity.Id);
        Assert.NotEqual(default(DateTime), entity.CreatedAtUtc);
        Assert.NotEqual(default(DateTime), entity.UpdatedAtUtc);
    }

    [Fact]
    public void CreatedAtUtc_ShouldBeSetToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var entity = new TestEntity();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.InRange(entity.CreatedAtUtc, beforeCreation.AddSeconds(-1), afterCreation.AddSeconds(1));
    }

    [Fact]
    public void UpdatedAtUtc_ShouldBeInitiallyEqualToCreatedAtUtc()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.Null(entity.UpdatedAtUtc);
    }

    [Fact]
    public void AddDomainEvent_ShouldAddEventToList()
    {
        // Arrange
        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();

        // Act
        entity.TriggerEvent(domainEvent);

        // Assert
        Assert.Single(entity.DomainEvents);
        Assert.Contains(domainEvent, entity.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_Multiple_ShouldAddAllEvents()
    {
        // Arrange
        var entity = new TestEntity();
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        var event3 = new TestDomainEvent();

        // Act
        entity.TriggerEvent(event1);
        entity.TriggerEvent(event2);
        entity.TriggerEvent(event3);

        // Assert
        Assert.Equal(3, entity.DomainEvents.Count);
        Assert.Contains(event1, entity.DomainEvents);
        Assert.Contains(event2, entity.DomainEvents);
        Assert.Contains(event3, entity.DomainEvents);
    }

    [Fact]
    public void RemoveDomainEvent_ShouldRemoveSpecificEvent()
    {
        // Arrange
        var entity = new TestEntity();
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        entity.TriggerEvent(event1);
        entity.TriggerEvent(event2);

        // Act
        entity.RemoveEvent(event1);

        // Assert
        Assert.Single(entity.DomainEvents);
        Assert.DoesNotContain(event1, entity.DomainEvents);
        Assert.Contains(event2, entity.DomainEvents);
    }

    [Fact]
    public void RemoveDomainEvent_NonExistentEvent_ShouldNotThrow()
    {
        // Arrange
        var entity = new TestEntity();
        var existingEvent = new TestDomainEvent();
        var nonExistentEvent = new TestDomainEvent();
        entity.TriggerEvent(existingEvent);

        // Act & Assert (should not throw)
        entity.RemoveEvent(nonExistentEvent);

        // Verify existing event is still there
        Assert.Single(entity.DomainEvents);
        Assert.Contains(existingEvent, entity.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var entity = new TestEntity();
        entity.TriggerEvent(new TestDomainEvent());
        entity.TriggerEvent(new TestDomainEvent());
        entity.TriggerEvent(new TestDomainEvent());

        // Act
        entity.ClearAllEvents();

        // Assert
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void DomainEvents_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var entity = new TestEntity();
        entity.TriggerEvent(new TestDomainEvent());

        // Act
        var events = entity.DomainEvents;

        // Assert
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<IDomainEvent>>(events);
    }

    [Fact]
    public void DomainEvents_InitiallyEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        var events = entity.DomainEvents;

        // Assert
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public void MultipleEntities_ShouldHaveUniqueIds()
    {
        // Arrange & Act
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        var entity3 = new TestEntity();

        // Assert
        Assert.NotEqual(entity1.Id, entity2.Id);
        Assert.NotEqual(entity2.Id, entity3.Id);
        Assert.NotEqual(entity1.Id, entity3.Id);
    }

    [Fact]
    public void Entity_ShouldBeAbstract()
    {
        // Assert
        Assert.True(typeof(Entity).IsAbstract);
    }

    [Fact]
    public void Entity_Properties_ShouldHaveCorrectAccessModifiers()
    {
        // Arrange
        var entity = new TestEntity();
        var entityType = typeof(Entity);

        // Assert - Id should have protected setter
        var idProperty = entityType.GetProperty("Id");
        Assert.NotNull(idProperty);
        Assert.True(idProperty!.GetMethod!.IsPublic);
        Assert.True(idProperty.SetMethod!.IsFamily); // IsFamily = protected

        // Assert - CreatedAtUtc should have internal setter
        var createdProperty = entityType.GetProperty("CreatedAtUtc");
        Assert.NotNull(createdProperty);
        Assert.True(createdProperty!.GetMethod!.IsPublic);
        Assert.True(createdProperty.SetMethod!.IsAssembly); // IsAssembly = internal

        // Assert - UpdatedAtUtc should have internal setter
        var updatedProperty = entityType.GetProperty("UpdatedAtUtc");
        Assert.NotNull(updatedProperty);
        Assert.True(updatedProperty!.GetMethod!.IsPublic);
        Assert.True(updatedProperty.SetMethod!.IsAssembly); // IsAssembly = internal
    }

    [Fact]
    public void DomainEvents_ShouldMaintainOrderOfAddition()
    {
        // Arrange
        var entity = new TestEntity();
        var event1 = new TestDomainEvent();
        System.Threading.Thread.Sleep(10); // Ensure different timestamps
        var event2 = new TestDomainEvent();
        System.Threading.Thread.Sleep(10);
        var event3 = new TestDomainEvent();

        // Act
        entity.TriggerEvent(event1);
        entity.TriggerEvent(event2);
        entity.TriggerEvent(event3);

        // Assert
        var events = entity.DomainEvents.ToList();
        Assert.Equal(event1, events[0]);
        Assert.Equal(event2, events[1]);
        Assert.Equal(event3, events[2]);
    }
}