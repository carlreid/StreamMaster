import { GetPagedChannelGroups } from '@lib/smAPI/ChannelGroups/ChannelGroupsCommands';
import { isSkipToken } from '@lib/common/isSkipToken';
import { Logger } from '@lib/common/logger';
import { createAsyncThunk } from '@reduxjs/toolkit';


export const fetchGetPagedChannelGroups = createAsyncThunk('cache/getGetPagedChannelGroups', async (query: string, thunkAPI) => {
  try {
    if (isSkipToken(query))
    {
        Logger.error('Skipping GetEPGFilePreviewById');
        return undefined;
    }
    if (query === undefined) return;
    const fetchDebug = localStorage.getItem('fetchDebug');
    const start = performance.now();
    const params = JSON.parse(query);
    const response = await GetPagedChannelGroups(params);
    if (fetchDebug) {
      const duration = performance.now() - start;
      Logger.debug(`Fetch GetPagedChannelGroups completed in ${duration.toFixed(2)}ms`);
    }
    return { query: query, value: response };
  } catch (error) {
    console.error('Failed to fetch', error);
    return thunkAPI.rejectWithValue({ error: error || 'Unknown error' });
  }
});


