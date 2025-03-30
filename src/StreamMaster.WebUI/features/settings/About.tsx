import { LinkButton } from "@components/buttons/LinkButton";
import SMButton from "@components/sm/SMButton";
import SMPopUp from "@components/sm/SMPopUp";
import { useSMContext } from "@lib/context/SMProvider";
import { memo, useEffect, useState } from "react";
import { GetMessage } from "@lib/common/intl";
import { Image } from "primereact/image";

interface Contributor {
  login: string;
  name?: string;
  avatar_url: string;
  html_url: string;
}

export interface SChannelMenuProperties {}

const About = () => {
  const { settings } = useSMContext();
  const [contributors, setContributors] = useState<Contributor[]>([]);

  useEffect(() => {
    import("@lib/contributors")
      .then(module => {
        setContributors(module.contributors || []);
      })
      .catch(error => {
        console.warn("Contributors data not found. Run the parse-contributors script first.");
        setContributors([]);
      });
  }, []);

  const originalCreators = contributors.filter(c => c.login === "mrmonday" || c.login === "senex");
  const otherContributors = contributors.filter(c => c.login !== "mrmonday" && c.login !== "senex");

  return (
    <SMPopUp
      modal
      modalCentered
      showClose={false}
      title="About"
      info=""
      placement="bottom"
      icon="pi-question"
      buttonClassName="icon-yellow"
      contentWidthSize="3"
    >
      <div className="flex flex-column sm-center-stuff">
        <div className="layout-padding-bottom" />
        <Image
          src="/images/streammaster_logo.png"
          alt="Stream Master"
          width="64"
        />
        Stream Master
        <div className="col-6 m-0 p-0 justify-content-center align-content-start text-xs text-center">
          <div className="sm-text-xs sm-center-stuff w-full">
            <LinkButton
              justText
              title={settings.Release ?? ""}
              link={
                "https://github.com/carlreid/StreamMaster/releases/tag/v" +
                settings.Release
              }
            />
          </div>
        </div>

        <div className="flex flex-column w-full sm-center-stuff">
          <div className="layout-padding-bottom" />

          {otherContributors.length > 0 && (
            <>
              <div className="text-center">
                <h3>Please thank all our wonderful contributors 🧡</h3>
              </div>
              <div 
                className="w-full" 
                style={{ 
                  maxHeight: '300px', 
                  overflowY: 'auto',
                  scrollbarWidth: 'thin'
                }}
              >
                <div className="flex flex-wrap justify-content-center gap-2 p-2">
                  {otherContributors.map((contributor) => (
                    <div key={contributor.login} className="flex flex-column align-items-center p-1">
                      <a 
                        href={contributor.html_url} 
                        target="_blank" 
                        rel="noopener noreferrer"
                        className="no-underline"
                      >
                        <Image 
                          src={contributor.avatar_url} 
                          alt={contributor.login || contributor.name} 
                          width="48" 
                          className="border-circle"
                        />
                        <div className="font-italic text-xs text-center mt-1" style={{ color: "var(--text-color)" }}>
                          {contributor.name || contributor.login}
                        </div>
                      </a>
                    </div>
                  ))}
                </div>
              </div>
              
            </>
          )}

          <div className="text-center text-xs mt-3 font-italic">
            <div>Special thanks to the original creators:</div>
            <div className="flex justify-content-center gap-3 mt-2">
              {originalCreators.length > 0 ? (
                originalCreators.map((creator) => (
                  <div key={creator.login} className="flex flex-column align-items-center">
                    <a 
                      href={creator.html_url} 
                      target="_blank" 
                      rel="noopener noreferrer"
                      className="no-underline"
                    >
                      <Image 
                        src={creator.avatar_url} 
                        alt={creator.name || creator.login} 
                        width="48" 
                      />
                    </a>
                  </div>
                ))
              ) : (
                <>
                  <div className="flex flex-column align-items-center">
                    <Image
                      src="/images/mrmonday_logo_sm.png"
                      alt="mrmonday_logo_sm"
                      width="48"
                    />
                    <div className="font-italic text-xs" style={{ color: "var(--text-color)" }}>
                      UI
                    </div>
                  </div>
                  <div className="flex flex-column align-items-center">
                    <Image src="/images/senex_logo_sm.png" alt="senex" width="48" />
                    <div className="font-italic text-xs" style={{ color: "var(--text-color)" }}>
                      Dev
                    </div>
                  </div>
                </>
              )}
            </div>
          </div>

          <div className="text-center text-xs mt-2 mb-2">
                <a 
                  href="https://github.com/carlreid/StreamMaster/blob/main/.github/CONTRIBUTING.md" 
                  target="_blank" 
                  rel="noopener noreferrer"
                  className="text-primary hover:text-primary-600"
                >
                  Learn more about contributing
                </a>
              </div>
          <div className="layout-padding-bottom" />
          <div className="w-full">
            <SMButton
              buttonDisabled={
                !settings.AuthenticationMethod ||
                settings.AuthenticationMethod === "None"
              }
              icon="pi-sign-out"
              label={GetMessage("signout")}
              onClick={() => (window.location.href = "/logout")}
              rounded
              iconFilled
              buttonClassName="icon-green"
            />
          </div>
        </div>
      </div>
    </SMPopUp>
  );
};

About.displayName = "About";

export default memo(About);
