﻿using BudgetAnalyser.Engine;
using Rees.Wpf.Contracts;

namespace BudgetAnalyser
{
    [AutoRegisterWithIoC(SingleInstance = true)]
    public class UserPrompts
    {
        public UserPrompts(
            [NotNull] IUserMessageBox messageBox,
            [NotNull] Func<IUserPromptOpenFile> openFileFactory,
            [NotNull] Func<IUserPromptSaveFile> saveFileFactory,
            [NotNull] IUserQuestionBoxYesNo yesNoBox,
            [NotNull] IUserInputBox inputBox)
        {
            if (messageBox is null)
            {
                throw new ArgumentNullException(nameof(messageBox));
            }

            if (openFileFactory is null)
            {
                throw new ArgumentNullException(nameof(openFileFactory));
            }

            if (saveFileFactory is null)
            {
                throw new ArgumentNullException(nameof(saveFileFactory));
            }

            if (yesNoBox is null)
            {
                throw new ArgumentNullException(nameof(yesNoBox));
            }

            if (inputBox is null)
            {
                throw new ArgumentNullException(nameof(inputBox));
            }

            OpenFileFactory = openFileFactory;
            SaveFileFactory = saveFileFactory;
            MessageBox = messageBox;
            YesNoBox = yesNoBox;
            InputBox = inputBox;
        }

        public IUserInputBox InputBox { get; private set; }
        public IUserMessageBox MessageBox { get; private set; }
        public Func<IUserPromptOpenFile> OpenFileFactory { get; private set; }
        public Func<IUserPromptSaveFile> SaveFileFactory { get; private set; }
        public IUserQuestionBoxYesNo YesNoBox { get; private set; }
    }
}
