<p align="center" width="100%">
    <img src="https://raw.githubusercontent.com/carlreid/StreamMaster/refs/heads/main/src/StreamMaster.WebUI/public/images/streammaster_logo.png" alt="StreamMaster Logo"/>
    <H1 align="center" width="100%">StreamMaster</H1>
</p>

> A comprehensive IPTV management platform for organizing and streaming public broadcast content through Plex DVR, Emby, and Jellyfin Live TV

## Quick Start 🚀

> [!TIP]  
> The wonderful [IPTV-org project](https://github.com/iptv-org/iptv) maintains a list of publicly available channels from all over the world 🌍📺 
> 
> TV logos are also publicly available in the [tv-logos repository](https://github.com/tv-logo/tv-logos)

Docker compose example:
```yaml
services:
  streammaster:
    image: ghcr.io/carlreid/streammaster:latest
    container_name: streammaster
    ports:
      - 7095:7095
    restart: unless-stopped
    volumes:
      - ~/streammaster:/config
      - ~/streammaster/tv-logos:/config/tv-logos
```

> [!NOTE]  
> You may also use `image: carlreid/streammaster:latest` if your platform can't pull from GitHub Container Registry (ghcr.io)

View available releases at our [container registry](https://github.com/carlreid/StreamMaster/pkgs/container/streammaster) (or on [Docker Hub](https://hub.docker.com/r/carlreid/streammaster))

## Key Features ⭐

- **Public IPTV Integration**: Easily manage free-to-air and public broadcast streams from sources like [iptv-org](https://iptv-org.github.io/)
- **M3U and EPG Management**: Import and organize multiple public playlists with automatic updates
- **Channel Organization**: Categorize streams by country, language, or content type
- **Logo Enhancement**: Cached channel logos with local directory support for consistent branding
- **Performance Analytics**: Monitor stream health and viewing statistics
- **Virtual HDHomeRun**: Create virtual tuners for better media server integration
- **Platform Compatibility**: Seamless integration with Plex, Emby, and Jellyfin
- **Modern Architecture**: Built with React and .NET for reliable performance
- **Smart Proxying**: RAM-based operations with fallback streams for reliability
- **Resource Optimization**: Single backend stream efficiently serves multiple devices

## Documentation 📚

[Find our documentation here](https://carlreid.github.io/StreamMaster/)

## Contributing 🤝

- **Issues**: Bug reports and feature requests ([create an issue](https://github.com/carlreid/StreamMaster/issues))
- **Discussions**: Community feedback and ideas ([open a discussion](https://github.com/carlreid/StreamMaster/discussions))
- **Development**: Pull requests welcome (read more about [how to contribute](https://github.com/carlreid/StreamMaster/blob/main/.github/CONTRIBUTING.md))

---

*This repository is a fork of the original SenexCrenshaw/StreamMaster project, which was discontinued(?) in early 2025.*

*For historical reference, see the original [README](README_old.md).*

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):
<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://www.carlreid.dk"><img src="https://avatars.githubusercontent.com/u/33623601?v=4?s=100" width="100px;" alt="Carl Reid"/><br /><sub><b>Carl Reid</b></sub></a><br /><a href="https://github.com/carlreid/StreamMaster/commits?author=carlreid" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://aandree5.github.io"><img src="https://avatars.githubusercontent.com/u/32734153?v=4?s=100" width="100px;" alt="Andre Silva"/><br /><sub><b>Andre Silva</b></sub></a><br /><a href="https://github.com/carlreid/StreamMaster/commits?author=Aandree5" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/iamfil"><img src="https://avatars.githubusercontent.com/u/329172?v=4?s=100" width="100px;" alt="Fil"/><br /><sub><b>Fil</b></sub></a><br /><a href="https://github.com/carlreid/StreamMaster/commits?author=iamfil" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/jackydec"><img src="https://avatars.githubusercontent.com/u/25207298?v=4?s=100" width="100px;" alt="jackydec"/><br /><sub><b>jackydec</b></sub></a><br /><a href="https://github.com/carlreid/StreamMaster/commits?author=jackydec" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/jbf154"><img src="https://avatars.githubusercontent.com/u/16122392?v=4?s=100" width="100px;" alt="jbf154"/><br /><sub><b>jbf154</b></sub></a><br /><a href="https://github.com/carlreid/StreamMaster/commits?author=jbf154" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/tam1m"><img src="https://avatars.githubusercontent.com/u/472185?v=4?s=100" width="100px;" alt="Tamim Baschour"/><br /><sub><b>Tamim Baschour</b></sub></a><br /><a href="https://github.com/carlreid/StreamMaster/commits?author=tam1m" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Austin1"><img src="https://avatars.githubusercontent.com/u/341408?v=4?s=100" width="100px;" alt="Austin1"/><br /><sub><b>Austin1</b></sub></a><br /><a href="https://github.com/carlreid/StreamMaster/commits?author=Austin1" title="Code">💻</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!