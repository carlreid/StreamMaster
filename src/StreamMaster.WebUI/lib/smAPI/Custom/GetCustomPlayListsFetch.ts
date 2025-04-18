import { GetCustomPlayLists } from '@lib/smAPI/Custom/CustomCommands';
import { Logger } from '@lib/common/logger';
import { createAsyncThunk } from '@reduxjs/toolkit';


export const fetchGetCustomPlayLists = createAsyncThunk('cache/getGetCustomPlayLists', async (_: void, thunkAPI) => {
  try {
    Logger.debug('Fetching GetCustomPlayLists');
    const fetchDebug = localStorage.getItem('fetchDebug');
    const start = performance.now();
    const response = await GetCustomPlayLists();
    if (fetchDebug) {
      const duration = performance.now() - start;
      Logger.debug(`Fetch GetCustomPlayLists completed in ${duration.toFixed(2)}ms`);
    }

    return {param: _, value: response };
  } catch (error) {
    console.error('Failed to fetch', error);
    return thunkAPI.rejectWithValue({ error: error || 'Unknown error', value: undefined });
  }
});


