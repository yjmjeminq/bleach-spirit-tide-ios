using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private sealed class NavFrame
        {
            public TideMode mode;public string npc,selected,owner,quest,routeTitle;
            public int journeyTab,journeyBoss;
            public int questAct,mainStep,mapAct,gearPage,gearSelection,gearFilter,gearSort,journalAct,journalPage,scene,line,indexPage,indexFilter,routeAct,routeStage,routeRoom;
            public bool side,dialogues,index;
        }
        private readonly Stack<NavFrame> navigation=new Stack<NavFrame>();
        private string pendingFocus;
        private int helpTab,helpSlot;
        private void PushContext()
        {
            GameObject selected=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            navigation.Push(new NavFrame{mode=Session.Mode,npc=dialogueNpc,selected=selected!=null?selected.name:null,
                journeyTab=journeyTab,journeyBoss=journeyBoss,questAct=questAct,mainStep=mainStep,owner=questOwner,quest=questSelected,side=questSide,
                mapAct=mapAct,gearPage=gearPage,gearSelection=gearSelection,gearFilter=gearFilter,gearSort=gearSort,
                journalAct=journalAct,journalPage=journalPage,scene=dialogueScene,line=dialogueLine,dialogues=journalDialogue,
                index=archiveIndex,indexPage=archivePage,indexFilter=archiveFilter,routeAct=routeAct,routeStage=routeStage,routeRoom=routeRoom,routeTitle=routeTitle});
        }
        private void OpenMenu(TideMode mode)
        {
            if(Session.Mode==mode)return;
            PushContext();
            if(mode==TideMode.Missions){ClearRoute();if(Session.Mode!=TideMode.Journal&&Session.Mode!=TideMode.Quests)mapAct=Session.Profile.campaignAct;}
            ResetInput();
            if(!Session.OpenMenu(mode)){navigation.Pop();return;}Refresh();
        }
        public void NavigateBack()
        {
            ResetInput();
            if(Session.Mode==TideMode.Story){CancelStory();return;}
            if(Session.Mode==TideMode.Title)return;
            if(Session.Mode==TideMode.ChooseLead){SetOpeningMode(TideMode.Title);return;}
            if(Session.Mode==TideMode.Result||Session.Mode==TideMode.Defeat){ReturnTown();return;}
            if(navigation.Count>0)
            {
                NavFrame previous=navigation.Pop();dialogueNpc=previous.npc;pendingFocus=previous.selected;
                journeyTab=previous.journeyTab;journeyBoss=previous.journeyBoss;questAct=previous.questAct;mainStep=previous.mainStep;questOwner=previous.owner;questSelected=previous.quest;questSide=previous.side;
                mapAct=previous.mapAct;gearPage=previous.gearPage;gearSelection=previous.gearSelection;gearFilter=previous.gearFilter;gearSort=previous.gearSort;
                journalAct=previous.journalAct;journalPage=previous.journalPage;dialogueScene=previous.scene;dialogueLine=previous.line;journalDialogue=previous.dialogues;
                archiveIndex=previous.index;archivePage=previous.indexPage;archiveFilter=previous.indexFilter;
                routeAct=previous.routeAct;routeStage=previous.routeStage;routeRoom=previous.routeRoom;routeTitle=previous.routeTitle;
                if(previous.mode==TideMode.Combat||previous.mode==TideMode.Training)Session.Resume();
                else {Session.Mode=previous.mode;Session.Revision++;}
            }
            else if(Session.IsPaused)Session.Resume();
            else {Session.Mode=TideMode.Town;Session.Revision++;}
            Refresh();
        }
        private void ToggleMainMenu()
        {
            if(Session.Mode==TideMode.Town||Session.IsFighting)OpenMenu(Session.InBattle?TideMode.Pause:TideMode.Home);
            else NavigateBack();
        }
        private void ResumeFromMenu(){navigation.Clear();Session.Resume();ResetInput();Refresh();}
        private void PreferencesChanged(){TideCatalog.Sanitize(Session.Profile);Persist();Session.Revision++;Refresh();}
        private void RequestRetreat(){if(Session.IsTraining)ReturnTown();else OpenMenu(TideMode.ConfirmRetreat);}
        private void ShowHelp(){helpTab=0;helpSlot=0;OpenMenu(TideMode.Help);}
        private bool HandleMenuKeyboard()
        {
            if(Session.IsMovingMode)return false;
            if(Input.GetKeyDown(KeyCode.Escape)){NavigateBack();return true;}
            if(Input.GetKeyDown(KeyCode.H)&&Session.Mode!=TideMode.Story&&Session.Mode!=TideMode.Result&&Session.Mode!=TideMode.Defeat&&Session.Mode!=TideMode.Title&&Session.Mode!=TideMode.ChooseLead){if(Session.Mode==TideMode.Help)NavigateBack();else ShowHelp();return true;}
            if(Input.GetKeyDown(KeyCode.Tab))CycleMenuFocus(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
            else if(Input.GetKeyDown(KeyCode.RightArrow))MoveMenuFocus(Vector2.right);
            else if(Input.GetKeyDown(KeyCode.LeftArrow))MoveMenuFocus(Vector2.left);
            else if(Input.GetKeyDown(KeyCode.UpArrow))MoveMenuFocus(Vector2.up);
            else if(Input.GetKeyDown(KeyCode.DownArrow))MoveMenuFocus(Vector2.down);
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space))ActivateMenuSelection();
            else if(Input.GetKeyDown(KeyCode.E))
            {if(Session.Mode==TideMode.Story)AdvanceStory(storyPage);else if(Session.Mode==TideMode.Dialogue)ActivateMenuSelection();}
            return true;
        }
        public void CycleMenuFocus(bool reverse)
        {
            if(EventSystem.current==null||Session.IsMovingMode)return;
            Transform root=dialogueRoot.activeSelf?dialogueRoot.transform:menuRoot.transform;
            var choices=new List<Button>();foreach(Button b in root.GetComponentsInChildren<Button>())if(b.interactable)choices.Add(b);
            if(choices.Count==0)return;
            int current=choices.FindIndex(b=>b.gameObject==EventSystem.current.currentSelectedGameObject);
            int next=current<0?0:(current+(reverse?-1:1)+choices.Count)%choices.Count;
            EventSystem.current.SetSelectedGameObject(choices[next].gameObject);
        }
        public void MoveMenuFocus(Vector2 direction)
        {
            if(EventSystem.current==null)return;
            Selectable current=EventSystem.current.currentSelectedGameObject!=null?EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>():null;
            if(current==null){FocusMenu();return;}
            Selectable next=Mathf.Abs(direction.x)>Mathf.Abs(direction.y)?direction.x>0?current.FindSelectableOnRight():current.FindSelectableOnLeft():direction.y>0?current.FindSelectableOnUp():current.FindSelectableOnDown();
            if(next!=null&&next.IsInteractable())EventSystem.current.SetSelectedGameObject(next.gameObject);
        }
        public void ActivateMenuSelection()
        {
            if(EventSystem.current==null||EventSystem.current.currentSelectedGameObject==null)return;
            Button selected=EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
            if(selected!=null&&selected.isActiveAndEnabled&&selected.interactable)selected.onClick.Invoke();
        }
        private void FocusMenu()
        {
            if(EventSystem.current==null||Session.IsMovingMode){EventSystem.current?.SetSelectedGameObject(null);return;}
            string preferred=pendingFocus;pendingFocus=null;
            if(string.IsNullOrEmpty(preferred))
            {
                switch(Session.Mode)
                {
                    case TideMode.Dialogue:case TideMode.Story:preferred="choice-0";break;
                    case TideMode.Title:preferred="title-start";break;
                    case TideMode.ChooseLead:preferred="lead-begin";break;
                    case TideMode.Home:preferred="home-primary";break;
                    case TideMode.Inventory:preferred="equip";break;
                    case TideMode.Forge:preferred="upgrade";break;
                    case TideMode.Journal:preferred=journalDialogue?"dialogue-records":"journal-map";break;
                    case TideMode.Quests:preferred=questSelected==null?"quest-side":"side-go";break;
                    case TideMode.Shop:preferred="buy";break;
                    case TideMode.Missions:preferred="depart";break;
                    case TideMode.Pause:case TideMode.Settings:preferred="resume";break;
                    case TideMode.Help:preferred="help-controls";break;
                    case TideMode.ConfirmRetreat:preferred="cancel-retreat";break;
                    case TideMode.Result:preferred="next-dungeon";break;
                    case TideMode.Defeat:preferred="retry";break;
                }
            }
            Button target;
            if(preferred!=null&&Buttons.TryGetValue(preferred,out target)&&target!=null&&target.gameObject.activeInHierarchy&&target.interactable){EventSystem.current.SetSelectedGameObject(target.gameObject);return;}
            Transform root=dialogueRoot.activeSelf?dialogueRoot.transform:menuRoot.transform;
            foreach(Button button in root.GetComponentsInChildren<Button>())if(button.interactable){EventSystem.current.SetSelectedGameObject(button.gameObject);return;}
        }
    }
}
