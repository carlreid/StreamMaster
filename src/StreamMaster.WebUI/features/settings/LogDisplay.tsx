import useGetLogContents from '@lib/smAPI/Logs/useGetLogContents';
import { GetLogContentsRequest } from '@lib/smAPI/smapiTypes';
import { ScrollPanel } from 'primereact/scrollpanel';
import { memo, useEffect } from 'react';

interface LogDisplayProperties {
  readonly LogName: string;
  onDataChange?: (logContent: string) => void;
}

const LogDisplay = ({ LogName, onDataChange }: LogDisplayProperties) => {
  const query = useGetLogContents({ LogName } as GetLogContentsRequest);

  const parsedJsonString = query.data
    ? query.data
        .replace(/\\u0022/g, '"') // Replace unicode for double quotes
        .replace(/\\r\\n/g, '\r\n') // Replace escaped newlines
        .replace(/\\\\/g, '\\')
    : ''; // Replace escaped backslashes

  useEffect(() => {
    if (parsedJsonString) {
      onDataChange?.(parsedJsonString);
    }
  }, [onDataChange, parsedJsonString]);

  if (query?.data === undefined || query.isLoading) {
    return <div>Loading...</div>;
  }

  if (query.isError) {
    return <div>Error loading logs</div>;
  }

  return (
    <ScrollPanel style={{ height: '80vh', width: '100%' }}>
      <pre style={{ whiteSpace: 'pre-wrap', wordWrap: 'break-word' }}>{parsedJsonString}</pre>
    </ScrollPanel>
  );
};

LogDisplay.displayName = 'LogDisplay';

export default memo(LogDisplay);
