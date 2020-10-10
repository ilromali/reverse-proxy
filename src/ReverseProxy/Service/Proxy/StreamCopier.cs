// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ReverseProxy.Service.Proxy
{
    /// <summary>
    /// A stream copier that captures errors.
    /// </summary>
    internal static class StreamCopier
    {
        // Taken from https://github.com/aspnet/Proxy/blob/816f65429b29d98e3ca98dd6b4d5e990f5cc7c02/src/Microsoft.AspNetCore.Proxy/ProxyAdvancedExtensions.cs#L19
        private const int DefaultBufferSize = 81920;

        /// <inheritdoc/>
        /// <remarks>
        /// Based on <c>Microsoft.AspNetCore.Http.StreamCopyOperationInternal.CopyToAsync</c>.
        /// See: <see href="https://github.com/dotnet/aspnetcore/blob/080660967b6043f731d4b7163af9e9e6047ef0c4/src/Http/Shared/StreamCopyOperationInternal.cs"/>.
        /// </remarks>
        public static async Task<(StreamCopyResult, Exception)> CopyAsync(Stream input, Stream output, CancellationToken cancellation)
        {
            _ = input ?? throw new ArgumentNullException(nameof(input));
            _ = output ?? throw new ArgumentNullException(nameof(output));

            // TODO: Consider System.IO.Pipelines for better perf (e.g. reads during writes)
            var buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
            long iops = 0;
            long totalBytes = 0;
            var reading = true;
            try
            {
                while (true)
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        return (StreamCopyResult.Canceled, new OperationCanceledException(cancellation));
                    }

                    iops++;
                    reading = true;
                    var read = await input.ReadAsync(buffer.AsMemory(), cancellation);

                    // End of the source stream.
                    if (read == 0)
                    {
                        return (StreamCopyResult.Success, null);
                    }

                    if (cancellation.IsCancellationRequested)
                    {
                        return (StreamCopyResult.Canceled, new OperationCanceledException(cancellation));
                    }

                    reading = false;
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellation);

                    totalBytes += read;
                }
            }
            catch (OperationCanceledException oex)
            {
                return (StreamCopyResult.Canceled, oex);
            }
            catch (Exception ex)
            {
                return (reading ? StreamCopyResult.InputError : StreamCopyResult.OutputError, ex);
            }
            finally
            {
                // We can afford the perf impact of clearArray == true since we only do this twice per request.
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }
    }
}
