﻿using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace MSBuildAbstractions
{
    public class MSBuildWorkspaceLoader
    {
        private readonly string _workspacePath;
        private readonly MSBuildWorkspaceType _workspaceType;

        public MSBuildWorkspaceLoader(string workspacePath, MSBuildWorkspaceType workspaceType)
        {
            if (string.IsNullOrWhiteSpace(workspacePath))
            {
                throw new ArgumentException($"{workspacePath} cannot be null or empty.");
            }

            if (!File.Exists(workspacePath))
            {
                throw new FileNotFoundException(workspacePath);
            }

            _workspacePath = workspacePath;
            _workspaceType = workspaceType;
        }

        public MSBuildWorkspace LoadWorkspace(string path, bool noBackup)
        {
            static bool IsSupportedProjectType(ProjectInSolution project)
            {
                if (project.ProjectType != SolutionProjectType.KnownToBeMSBuildFormat)
                {
                    Console.WriteLine($"{project.AbsolutePath} is not a supported project type and will be skipped.");
                }

                return project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat;
            }

            var projectPaths =
                _workspaceType switch
                {
                    MSBuildWorkspaceType.Project => ImmutableArray.Create(path),
                    MSBuildWorkspaceType.Solution =>
                        SolutionFile.Parse(_workspacePath).ProjectsInOrder
                            .Where(IsSupportedProjectType)
                            .Select(p => p.AbsolutePath).ToImmutableArray(),
                    _ => throw new InvalidOperationException("couldn't do literally anything")
                };

            return new MSBuildWorkspace(projectPaths, noBackup);
        }

        public IProjectRootElement GetRootElementFromProjectFile(string projectFilePath = "")
        {
            var path = Path.GetFullPath(projectFilePath);

            if (!File.Exists(path))
            {
                throw new ArgumentException($"The project file '{projectFilePath}' does not exist or is inaccessible.");
            }

            using var collection = new ProjectCollection();

            return new MSBuildProjectRootElement(ProjectRootElement.Open(path, collection, preserveFormatting: true));
        }
    }
}