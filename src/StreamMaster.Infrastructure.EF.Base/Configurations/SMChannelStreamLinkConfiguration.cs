using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StreamMaster.Infrastructure.EF.Base.Configurations
{
    public class SMChannelStreamLinkConfiguration : IEntityTypeConfiguration<SMChannelStreamLink>
    {
        public void Configure(EntityTypeBuilder<SMChannelStreamLink> entity)
        {
            entity.HasKey(channelStreamLink => new
            {
                channelStreamLink.SMChannelId,
                channelStreamLink.SMStreamId,
                channelStreamLink.SMStreamM3UFileId
            });

            entity.HasOne(vsl => vsl.SMChannel)
                  .WithMany(vs => vs.SMStreams)
                  .HasForeignKey(vsl => vsl.SMChannelId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}