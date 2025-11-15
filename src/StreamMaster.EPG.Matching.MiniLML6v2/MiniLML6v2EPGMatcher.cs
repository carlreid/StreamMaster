using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using StreamMaster.Domain.EPG;
using StreamMaster.Domain.Helpers;
using StreamMaster.Domain.Models;
using StreamMaster.Domain.XmltvXml;
using StreamMaster.Streams.Domain.Interfaces;

namespace StreamMaster.EPG.Matching.MiniLML6v2
{
    public class MiniLML6v2EPGMatcher : IEpgMatcher, IDisposable
    {
        private readonly ICacheManager _cacheManager;
        private readonly InferenceSession _session;
        private readonly BertTokenizer _tokenizer;
        private readonly Dictionary<string, float[]> _cachedEmbeddings = new();
        private readonly SemaphoreSlim _cacheLock = new(1, 1);
        private bool _disposed = false;

        private const int TotalDimensions = 384;
        private const int MaxSequenceLength = 128;

        public MiniLML6v2EPGMatcher(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model.onnx");
            string vocabPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vocab.txt");

            _session = new InferenceSession(modelPath);

            var options = new BertOptions
            {
                // Base WordPieceOptions
                UnknownToken = "[UNK]",
                ContinuingSubwordPrefix = "##",
                MaxInputCharsPerWord = 100,
                LowerCaseBeforeTokenization = true,
                ApplyBasicTokenization = true,
                SplitOnSpecialTokens = true,
                SeparatorToken = "[SEP]",
                PaddingToken = "[PAD]",
                ClassificationToken = "[CLS]",
                MaskingToken = "[MASK]",
                IndividuallyTokenizeCjk = true,
                RemoveNonSpacingMarks = false
            };

            _tokenizer = BertTokenizer.Create(vocabPath, options);
        }

        public async Task<StationChannelName?> MatchAsync(SMChannel channel, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Try direct match first if EPGID is valid
            if (EPGHelper.TryExtractEPGNumberAndStationId(channel.EPGId, out int epgNumber, out string stationId))
            {
                if (_cacheManager.StationChannelNames.TryGetValue(epgNumber, out List<StationChannelName>? channelList))
                {
                    // Exact match by channel/stationId
                    StationChannelName? exactMatch = channelList.FirstOrDefault(c =>
                        c.Channel.Equals(stationId, StringComparison.OrdinalIgnoreCase));

                    if (exactMatch is not null)
                    {
                        return exactMatch;
                    }

                    // If exact match not found, try semantic matching
                    StationChannelName? bestMatch = await FindBestSemanticMatchAsync(channel, channelList, cancellationToken);
                    if (bestMatch is not null)
                    {
                        return bestMatch;
                    }
                }
            }

            // If we cannot find by EPGID (or it's dummy), try all channels in all EPGs
            List<StationChannelName> allChannels = _cacheManager.StationChannelNames.Values
                .SelectMany(v => v)
                .ToList();

            return await FindBestSemanticMatchAsync(channel, allChannels, cancellationToken);
        }

        private async Task<StationChannelName?> FindBestSemanticMatchAsync(
            SMChannel channel,
            IEnumerable<StationChannelName> candidates,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                string channelName = !string.IsNullOrEmpty(channel.TVGName)
                    ? channel.TVGName
                    : channel.Name ?? string.Empty;

                if (string.IsNullOrWhiteSpace(channelName))
                {
                    return null;
                }

                // Get embedding for the channel name
                float[] channelEmbedding = GetEmbedding(channelName);

                // Find the best match by cosine similarity
                float bestScore = -1;
                StationChannelName? bestMatch = null;

                foreach (var candidate in candidates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Try matching against different fields
                    float displayNameScore = GetCosineSimilarity(channelEmbedding, GetEmbedding(candidate.DisplayName));
                    float channelNameScore = GetCosineSimilarity(channelEmbedding, GetEmbedding(candidate.ChannelName));
                    float channelScore = GetCosineSimilarity(channelEmbedding, GetEmbedding(candidate.Channel));

                    // Take the best score among the fields
                    float bestCandidateScore = Math.Max(displayNameScore, Math.Max(channelNameScore, channelScore));

                    if (bestCandidateScore > bestScore)
                    {
                        bestScore = bestCandidateScore;
                        bestMatch = candidate;
                    }
                }

                // Only return matches with reasonable similarity
                return bestScore > 0.6f ? bestMatch : null;
            }, cancellationToken);
        }

        private float[] GetEmbedding(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new float[TotalDimensions];
            }

            // Check if we already have this embedding cached
            _cacheLock.Wait();
            try
            {
                if (_cachedEmbeddings.TryGetValue(text, out float[]? embeddingResult))
                {
                    return embeddingResult;
                }
            }
            finally
            {
                _cacheLock.Release();
            }

            // Set up encode settings with the correct properties
            var encodeSettings = new EncodeSettings
            {
                ConsiderNormalization = true,
                ConsiderPreTokenization = true,
                MaxTokenCount = MaxSequenceLength
            };

            // Tokenize the text - we need to handle special tokens manually
            // First get the token IDs for the text
            var ids = new List<int>();

            // Add [CLS] token at the beginning
            ids.Add(_tokenizer.ClassificationTokenId);

            // Add the actual text tokens
            var textIds = _tokenizer.EncodeToIds(text).ToList();

            // Truncate if needed to leave room for [SEP] token
            if (textIds.Count > MaxSequenceLength - 2) // -2 for [CLS] and [SEP]
            {
                textIds = textIds.Take(MaxSequenceLength - 2).ToList();
            }

            ids.AddRange(textIds);

            // Add [SEP] token at the end
            ids.Add(_tokenizer.SeparatorTokenId);

            // Create attention mask (1 for real tokens, 0 for padding)
            var attentionMask = Enumerable.Repeat(1L, ids.Count).ToList();

            // Create token type ids (all 0 for single sequence)
            var tokenTypeIds = Enumerable.Repeat(0L, ids.Count).ToList();

            // Pad sequences to max length if needed
            int paddingLength = MaxSequenceLength - ids.Count;
            if (paddingLength > 0)
            {
                ids.AddRange(Enumerable.Repeat(_tokenizer.PaddingTokenId, paddingLength));
                attentionMask.AddRange(Enumerable.Repeat(0L, paddingLength));
                tokenTypeIds.AddRange(Enumerable.Repeat(0L, paddingLength));
            }

            // Create input tensors
            var inputIdsTensor = new DenseTensor<long>(new[] { 1, ids.Count });
            var attentionMaskTensor = new DenseTensor<long>(new[] { 1, attentionMask.Count });
            var tokenTypeIdsTensor = new DenseTensor<long>(new[] { 1, tokenTypeIds.Count });

            // Fill tensors
            for (int i = 0; i < ids.Count; i++)
            {
                inputIdsTensor[0, i] = ids[i];
                attentionMaskTensor[0, i] = attentionMask[i];
                tokenTypeIdsTensor[0, i] = tokenTypeIds[i];
            }

            // Create inputs
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
            };

            // Run inference
            using var results = _session.Run(inputs);

            // Get the embedding - we need to do mean pooling like in the Python example
            // Check the actual output name in your ONNX model
            var outputName = results.First().Name; // This might be "last_hidden_state" or something else
            var lastHiddenState = results.First().AsTensor<float>();
            var embedding = MeanPooling(lastHiddenState, attentionMaskTensor);

            // Normalize the embedding (L2 norm)
            embedding = NormalizeL2(embedding);

            // Cache the result
            _cacheLock.Wait();
            try
            {
                _cachedEmbeddings[text] = embedding;
            }
            finally
            {
                _cacheLock.Release();
            }

            return embedding;
        }

        private float[] MeanPooling(Tensor<float> tokenEmbeddings, Tensor<long> attentionMask)
        {
            int batchSize = tokenEmbeddings.Dimensions[0];
            int seqLength = tokenEmbeddings.Dimensions[1];
            int hiddenSize = tokenEmbeddings.Dimensions[2];

            var result = new float[hiddenSize];

            // Sum token embeddings weighted by attention mask
            float sum = 0;
            for (int i = 0; i < seqLength; i++)
            {
                if (attentionMask[0, i] == 1)
                {
                    sum += 1;
                    for (int j = 0; j < hiddenSize; j++)
                    {
                        result[j] += tokenEmbeddings[0, i, j];
                    }
                }
            }

            // Average by dividing by the sum of the attention mask
            if (sum > 0)
            {
                for (int j = 0; j < hiddenSize; j++)
                {
                    result[j] /= sum;
                }
            }

            return result;
        }

        private float[] NormalizeL2(float[] vector)
        {
            float norm = 0;
            for (int i = 0; i < vector.Length; i++)
            {
                norm += vector[i] * vector[i];
            }

            norm = (float)Math.Sqrt(norm);

            if (norm > 0)
            {
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] /= norm;
                }
            }

            return vector;
        }

        private float GetCosineSimilarity(float[] v1, float[] v2)
        {
            float dotProduct = 0;
            float mag1 = 0;
            float mag2 = 0;

            for (int i = 0; i < v1.Length; i++)
            {
                dotProduct += v1[i] * v2[i];
                mag1 += v1[i] * v1[i];
                mag2 += v2[i] * v2[i];
            }

            mag1 = (float)Math.Sqrt(mag1);
            mag2 = (float)Math.Sqrt(mag2);

            if (mag1 == 0 || mag2 == 0)
                return 0;

            return dotProduct / (mag1 * mag2);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _session?.Dispose();
                    _cacheLock?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}