﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using IOleCommandTarget = Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
    using OLECMDEXECOPT = Microsoft.VisualStudio.OLE.Interop.OLECMDEXECOPT;
    using Task = System.Threading.Tasks.Task;

    [TestService]
    internal partial class ShellInProcess
    {
        public new Task<TInterface> GetRequiredGlobalServiceAsync<TService, TInterface>(CancellationToken cancellationToken)
            where TService : class
            where TInterface : class
        {
            return base.GetRequiredGlobalServiceAsync<TService, TInterface>(cancellationToken);
        }

        public new Task<TService> GetComponentModelServiceAsync<TService>(CancellationToken cancellationToken)
            where TService : class
        {
            return base.GetComponentModelServiceAsync<TService>(cancellationToken);
        }

        public async Task<CommandID> PrepareCommandAsync(string command, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var commandWindow = await GetRequiredGlobalServiceAsync<SVsCommandWindow, IVsCommandWindow>(cancellationToken);
            var result = new PREPARECOMMANDRESULT[1];
            ErrorHandler.ThrowOnFailure(commandWindow.PrepareCommand(command, out var commandGroup, out var commandId, out var cmdArg, result));

            Marshal.FreeCoTaskMem(cmdArg);

            return new CommandID(commandGroup, (int)commandId);
        }

        public async Task ExecuteCommandAsync(string command, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var commandID = await PrepareCommandAsync(command, cancellationToken);
            await ExecuteCommandAsync(commandID, cancellationToken);
        }

        public Task ExecuteCommandAsync(CommandID command, CancellationToken cancellationToken)
            => ExecuteCommandAsync(command.Guid, (uint)command.ID, cancellationToken);

        public Task ExecuteCommandAsync(CommandID command, string argument, CancellationToken cancellationToken)
            => ExecuteCommandAsync(command.Guid, (uint)command.ID, argument, cancellationToken);

        public async Task ExecuteCommandAsync(Guid commandGuid, uint commandId, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dispatcher = await TestServices.Shell.GetRequiredGlobalServiceAsync<SUIHostCommandDispatcher, IOleCommandTarget>(cancellationToken);
            ErrorHandler.ThrowOnFailure(dispatcher.Exec(commandGuid, commandId, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero));
        }

        public async Task ExecuteCommandAsync(Guid commandGuid, uint commandId, string argument, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dispatcher = await TestServices.Shell.GetRequiredGlobalServiceAsync<SUIHostCommandDispatcher, IOleCommandTarget>(cancellationToken);

            var pvaIn = Marshal.AllocHGlobal(Marshal.SizeOf<VariantHelper>());
            try
            {
                Marshal.GetNativeVariantForObject(argument, pvaIn);
                ErrorHandler.ThrowOnFailure(dispatcher.Exec(commandGuid, commandId, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, pvaIn, IntPtr.Zero));
            }
            finally
            {
                var variant = Marshal.PtrToStructure<VariantHelper>(pvaIn);
                variant.Clear();
                Marshal.FreeHGlobal(pvaIn);
            }
        }

        public async Task<string> GetActiveWindowCaptionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var monitorSelection = await GetRequiredGlobalServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>(cancellationToken);
            ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out var windowFrameObj));
            var windowFrame = (IVsWindowFrame)windowFrameObj;

            ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_Caption, out var captionObj));
            return $"{captionObj}";
        }

        public async Task<Version> GetVersionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = await GetRequiredGlobalServiceAsync<SVsShell, IVsShell>(cancellationToken);
            shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out var versionProperty);

            var fullVersion = versionProperty?.ToString() ?? string.Empty;
            var firstSpace = fullVersion.IndexOf(' ');
            if (firstSpace >= 0)
            {
                // e.g. "17.1.31907.60 MAIN"
                fullVersion = fullVersion.Substring(0, firstSpace);
            }

            if (Version.TryParse(fullVersion, out var version))
            {
                return version;
            }

            throw new NotSupportedException($"Unexpected version format: {versionProperty}");
        }
    }
}
