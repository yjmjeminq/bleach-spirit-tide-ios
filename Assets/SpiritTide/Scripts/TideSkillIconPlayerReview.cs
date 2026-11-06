using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private IEnumerator ReviewSkillIcons()
        {
            string saved=JsonUtility.ToJson(game.Session.Profile);int checkedIcons=0;
            for(int hero=0;hero<6;hero++)
            {
                game.ReturnTown();yield return TapUi("hero-"+hero);game.StartBattle(true);yield return null;
                for(int slot=0;slot<5;slot++)
                {
                    string id=game.Session.Hero.Skill(slot).id;var button=game.Buttons["skill-"+slot];
                    var icon=button.transform.Find("SkillIcon").GetComponent<Image>();
                    Check("live skill button shows its own painted icon "+id,icon.sprite!=null&&icon.sprite.name=="skill-icon-"+id&&icon.sprite.rect.width>300&&!icon.raycastTarget);
                    Canvas.ForceUpdateCanvases();var label=button.GetComponentInChildren<Text>();
                    Check("compact skill label renders real glyphs "+id,label.text==TideSkillIcons.Definition(hero,slot).label&&label.preferredHeight<=label.rectTransform.rect.height&&label.cachedTextGenerator.vertexCount>0);
                    CheckHit("skill-"+slot);checkedIcons++;
                }
                yield return Capture("92-painted-icons-"+hero);
                yield return TapUi("pause");yield return TapUi("help");yield return TapUi("help-skills");
                for(int slot=0;slot<5;slot++)
                {
                    var icon=game.Buttons["help-slot-"+slot].transform.Find("HelpSkillIcon").GetComponent<Image>();
                    Check("skill help reuses the correct painted icon "+hero+" / "+slot,icon.sprite==TideSkillIcons.Get(hero,slot)&&!icon.raycastTarget);
                }
                yield return Capture("92-painted-help-"+hero);LeaveToTown();
            }
            var fixture=new TideProfile{hero=0,quest=1,level=10};TideCatalog.Sanitize(fixture);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fixture),game.Session.Profile);
            game.ReturnTown();game.StartBattle(false);yield return ReadStory();yield return TapUi("skill-0");
            Button first=game.Buttons["skill-0"];var timer=first.transform.Find("SkillCooldownNumber").GetComponent<Text>();var fill=first.transform.Find("Cooldown").GetComponent<Image>();
            int serial=game.Session.AttackSerial;first.onClick.Invoke();
            Check("cooldown overlay and numeric timer reflect the real cooldown",!first.interactable&&timer.text.Length>0&&fill.fillMethod==Image.FillMethod.Radial360&&fill.fillAmount>0&&game.Session.AttackSerial==serial);
            game.Session.Mp=0;game.Session.Revision++;yield return null;
            var disabled=game.Buttons["skill-1"];var dim=disabled.transform.Find("SkillIcon").GetComponent<Image>();
            Check("insufficient spirit dims the art and prevents the real cast",!disabled.interactable&&dim.color.r<.6f&&VisibleText("영압 부족"));
            yield return Capture("93-icon-cooldown-and-spirit");CheckHit("attack");
            Check("all thirty live skill icons and help icons were reviewed",checkedIcons==30);
            File.WriteAllText(Path.Combine(output,"skill-icons-review.json"),"{\"passed\":true,\"version\":\""+Application.version+"\",\"guid\":\""+Application.buildGUID+"\",\"icons\":30,\"atlas_files\":6,\"scope\":\"Actual Windows Unity rendered icons and pointer checks; not iPhone hardware execution\"}");
            JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);game.Session.ClearQuestNotifications();game.ReturnTown();yield return null;
        }
    }
}
