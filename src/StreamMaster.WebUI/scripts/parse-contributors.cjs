const fs = require('fs');
const path = require('path');

const README_PATH = path.join(__dirname, '../../../README.md');
const OUTPUT_PATH = path.join(__dirname, '../lib/contributors.js');

function parseContributorsFromReadme() {
    try {
        const readmeContent = fs.readFileSync(README_PATH, 'utf8');

        // Look for the contributors section with the updated pattern
        const contributorsTableRegex = /<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->([\s\S]*?)<!-- ALL-CONTRIBUTORS-LIST:END -->/;
        const match = readmeContent.match(contributorsTableRegex);

        if (!match) {
            console.warn('Contributors section not found in README.md');
            return [];
        }

        const contributorsTable = match[1];

        // Parse the contributors from the table
        // Updated regex to match the actual HTML structure in the README
        const contributorRegex = /<a href="([^"]+)"><img src="([^"]+)\?[^"]*" width="[^"]*" alt="([^"]*)"\/?><br \/><sub><b>([^<]+)<\/b><\/sub><\/a>/g;

        const contributors = [];
        let contributorMatch;

        while ((contributorMatch = contributorRegex.exec(contributorsTable)) !== null) {
            const url = contributorMatch[1];
            const login = url.split('/').pop();

            contributors.push({
                html_url: url,
                login: login,
                avatar_url: contributorMatch[2],
                name: contributorMatch[4]
            });
        }

        return contributors;
    } catch (error) {
        console.error('Error parsing contributors from README:', error);
        return [];
    }
}

function main() {
    try {
        console.log('Parsing contributors from README.md...');
        const contributors = parseContributorsFromReadme();

        if (contributors.length === 0) {
            console.warn('No contributors found. Make sure the README.md has an allcontributors section.');

            // Create a fallback with at least the original creators
            const fallbackContributors = [
                {
                    login: "mrmonday",
                    name: "Mr Monday",
                    avatar_url: "/images/mrmonday_logo_sm.png",
                    html_url: "https://github.com/mrmonday",
                },
                {
                    login: "senex",
                    name: "Senex",
                    avatar_url: "/images/senex_logo_sm.png",
                    html_url: "https://github.com/senex",
                }
            ];

            writeContributorsFile(fallbackContributors);
            return;
        }

        writeContributorsFile(contributors);
        console.log(`Successfully parsed ${contributors.length} contributors from README.md`);
    } catch (error) {
        console.error('Error in main function:', error);
        process.exit(1);
    }
}

function writeContributorsFile(contributors) {
    const outputDir = path.dirname(OUTPUT_PATH);
    if (!fs.existsSync(outputDir)) {
        fs.mkdirSync(outputDir, { recursive: true });
    }

    // Write the contributors data to a JavaScript file
    const fileContent = `// This file is auto-generated. Do not edit manually.
// To update this, run "npm run give-credit" in the WebUI project.
// Last updated: ${new Date().toISOString()}

export const contributors = ${JSON.stringify(contributors, null, 2)};
`;

    fs.writeFileSync(OUTPUT_PATH, fileContent);
}

main();
