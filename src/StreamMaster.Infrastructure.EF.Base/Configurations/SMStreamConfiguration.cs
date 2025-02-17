using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StreamMaster.Infrastructure.EF.Base.Configurations
{
    public class SMStreamConfiguration : IEntityTypeConfiguration<SMStream>
    {
        public void Configure(EntityTypeBuilder<SMStream> modelBuilder)
        {
            modelBuilder.HasKey(stream => stream.Id);
        }
    }
}