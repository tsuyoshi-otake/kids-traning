namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildProgressionScript()
    {
        return """
skillAverage(p){const values=Object.values((p&&p.mastery)||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));return values.length?values.reduce((a,b)=>a+b,0)/values.length:0.05;}
  pickStage(stage,buckets,reviewRate=.25){const top=Math.max(1,Math.min(5,Number(stage)||1));let current=top-1;while(current>0&&!(buckets[current]&&buckets[current].length))current--;const active=(buckets[current]||[]).map(fn=>({fn:fn,difficulty:current+1})),previous=[];for(let i=0;i<current;i++)for(const fn of (buckets[i]||[]))previous.push({fn:fn,difficulty:i+1});const pool=previous.length&&Math.random()<reviewRate?previous:(active.length?active:previous);if(!pool.length)throw new Error('No questions configured for stage '+top);const picked=pool[this.rand(0,pool.length-1)],q=picked.fn();q.difficulty=picked.difficulty;return q;}
  effectiveGrade(p){const base=Math.max(1,Math.min(3,Number(p&&p.grade)||1));const values=Object.values((p&&p.mastery)||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));const top=values.length?Math.max(...values):0.05,stars=Number(p&&p.stars)||0;const byProgress=stars>=150&&top>=0.65?3:(stars>=55&&top>=0.45?2:1);return Math.max(base,byProgress);}
  gradeLabel(p){const base=Math.max(1,Math.min(3,Number(p&&p.grade)||1)),g=this.effectiveGrade(p);return g+'年生'+(g>base?' 範囲':'');}
  learningStage(p){const level=this.skillLevel(p),stars=Number(p.stars)||0;if(stars<15&&level<=1)return 1;if(stars<45||level<=2)return 2;if(stars<90||level<=3)return 3;if(stars<150||level<=4)return 4;return 5;}
  topicStage(p,k){const m=Number((p&&p.mastery&&p.mastery[k])||0.05);if(m<0.20)return 1;if(m<0.40)return 2;if(m<0.60)return 3;if(m<0.80)return 4;return 5;}
  reviewStage(p,k){const stage=this.topicStage(p,k);return stage>1&&Math.random()<0.25?this.rand(1,stage-1):stage;}
  profileAtStage(p,k,stage){const levels=[0.05,0.25,0.45,0.65,0.85],mastery={...((p&&p.mastery)||{})};mastery[k]=levels[Math.max(1,Math.min(5,stage))-1];return{...p,mastery:mastery};}
  topicComplete(p,k){if(p&&p.cleared&&p.cleared[k])return true;return this.topicStage(p,k)>=5;}
  markCleared(p,k){if(this.topicStage(p,k)>=5)(p.cleared=p.cleared||{})[k]=true;}
  hissanComplete(p){return this.topicComplete(p,'hissan');}
  allowedTopics(p){const all=Object.keys(this.topics);const cfg=this.state.settings;const en=(cfg&&cfg.topics)?all.filter(k=>cfg.topics[k]):all;const enabled=en.length?en:all;const grade=this.effectiveGrade(p),done=k=>this.topicComplete(p,k);const staged=['add'];if(done('add'))staged.push('sub','moji');if(done('sub'))staged.push('kazu','clock','story');if(done('moji'))staged.push('kokugo','bun');if(done('bun'))staged.push('goi');if(done('kokugo'))staged.push('dokkai');if(grade>=3&&done('moji'))staged.push('eigo');if(done('kazu'))staged.push('measure','chart');if(done('measure'))staged.push('shape');if(grade>=2&&done('kazu'))staged.push('hissan');if(grade>=2&&done('hissan'))staged.push('mul');if(grade>=2&&done('mul'))staged.push('frac');if(grade>=3&&done('mul'))staged.push('div');const allowed=staged.filter(k=>enabled.includes(k));return allowed.length?allowed:staged;}
  weightedPick(p){const ks=this.allowedTopics(p);const w=ks.map(k=>{let base=0.25+(1-(Number(p.mastery[k])||0.05))*1.7;if(k==='hissan'&&!this.hissanComplete(p))base*=1.25;if(k==='mul'&&this.topicStage(p,'mul')<=1)base*=0.7;return base;});let s=w.reduce((a,b)=>a+b,0),r=Math.random()*s;for(let i=0;i<ks.length;i++){r-=w[i];if(r<=0)return ks[i];}return ks[0];}
""";
    }

}