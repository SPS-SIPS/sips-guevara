namespace SPSS.Domain.Models.BL;

public sealed class MonitHistory : BaseEntity<int>
{
    public Participant Participant { get; set; } = default!;
    public string ParticipantId { get; set; } = default!;
    public DateTimeOffset Date { get; set; }
    public ServiceType Type { get; set; } = ServiceType.VPN;
    public ServiceStatus Status { get; set; } = ServiceStatus.Up;
    public string Address { get; set; } = default!;
    public long RoundtripTime { get; set; }
    public int PacketLoss { get; set; }
    public string? AdditionalInfo { get; set; }

    public bool Sent { get; set; } = false;
}

public sealed class MonitHistoryConfiguration : IEntityTypeConfiguration<MonitHistory>
{
    public void Configure(EntityTypeBuilder<MonitHistory> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Type).HasConversion(x => x.Id, x => Enumeration<int>.FromId<ServiceType>(x));
        builder.Property(e => e.Status).HasConversion(x => x.Id, x => Enumeration<int>.FromId<ServiceStatus>(x));

        builder.HasOne(e => e.Participant)
            .WithMany(e => e.MonitHistories)
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}