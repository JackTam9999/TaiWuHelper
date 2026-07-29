# M1-007 effective skill-cost evidence

## Verified rule

The supplied screenshots identify the inner-power legendary book as
`內功·浮心無字訣`. Of its six shown effects, only `收置` changes the occupied
skill cost:

- A skill placed in `收置` occupies exactly one skill slot.
- Its activation requirements increase by 50%; requirement modelling belongs
  to a later recommendation slice and is not silently treated as a cost.
- This is a fixed occupied cost, not an arbitrary subtraction from `GridCost`.
- Applying it to a skill already costing one cannot increase or further reduce
  that cost.

The cost calculator therefore starts with configured `GridCost`, applies the
confirmed mastery reduction with a minimum of one, and then replaces the
occupied cost with the evidence-backed `收置` fixed cost of one. Because the
rule is exact, that final cost remains known even when the ordinary grid cost
or mastery value is unavailable; only the amount reduced is then unknown.

## Replaceable active-skill assignments

The skill named after `生效功法` in each screenshot is the current selection,
not a permanent binding between that effect and that skill. The user confirmed
that this active skill can be changed.

The helper therefore separates the verified effect from the selected skill in
its behaviour:

- A current snapshot applies `收置` only to the skill currently assigned to it.
- A verified book effect is an owned `LegendaryBookCostSlot`; its current
  selected skill is a separate `LegendaryBookCostAssignment`.
- A recommendation may evaluate a different learned skill by calling
  `ProposeForSkill`, which creates a `Proposed` helper-side assignment with
  its own proposal evidence.
- Proposed evaluation requires an owned slot and leaves the current snapshot
  assignment unchanged.
- The helper only reports the proposed manual change. It does not change the
  save, game process, or live selection.

## Owned-book coverage boundary

The user confirmed that the four supplied legendary books are the complete
set currently owned:

- `內功·浮心無字訣`
- `身法·白衣行化笈`
- The supplied assistance legendary book
- `刀法·十余魔羅錄`

Screenshots for other books are neither available nor required for the current
player. The helper must not infer their effects, advertise them as available,
or block recommendations waiting for evidence that the player cannot provide.
If ownership changes later, the newly available book can be verified then.

## Deliberately excluded effects

| Effect | Screenshot evidence | Why it is not an M1-007 cost modifier |
|---|---|---|
| `用極` | Local-only `M1-007-yongji-power.png` | Changes skill power according to the five-element relationship. |
| `大盈` | Local-only `M1-007-daying-grid-tradeoff.png` | Reduces the four category-grid contributions by one and adds three generic grids; this belongs to slot-budget calculation. |
| `專解` | Local-only `M1-007-zhuanjie-power.png` | Raises the power limit and activation requirements. |
| `大成` | Local-only `M1-007-dacheng-grid-tradeoff.png` | Adds one to the four category-grid contributions and removes three generic grids; this belongs to slot-budget calculation. |
| `絕旨` | Local-only `M1-007-juezhi-combat-power.png` | Changes user and enemy skill power at combat start. |

The local-only `M1-007-fuxin-wuzijue.png` book identity and
`M1-007-shouzhi-fixed-cost.png` fixed-cost evidence are retained outside Git
alongside these exclusions. No rule is inferred beyond the visible text.

## Independent agility-book confirmation

The local-only `M1-007-baiyi-xinghua-ji.png` evidence independently confirms
the `身法·白衣行化笈` identity and the same `收置` wording for an agility
skill. The local-only `M1-007-agility-shouzhi-fixed-cost.png` evidence shows
`芝蘭玉步` as the current selection, occupying exactly one skill slot and
increasing activation requirements by 50%.

This confirms that fixed-cost modelling is not specific to inner power or the
attack category. The Domain calculator therefore remains category-agnostic
while still requiring an exact skill/category match.

The other visible `白衣行化笈` effects are not occupied-cost modifiers:

| Effect | Screenshot evidence | Separate concern |
|---|---|---|
| `鳳身` | Local-only `M1-007-fengshen-activation.png` | Makes the placed skill activate automatically at combat start without consumption or preparation. |
| `雲湧` | Local-only `M1-007-yunyong-footwork.png` | Reduces movement footwork consumption by 25% during the skill. |
| `專解` | Local-only `M1-007-agility-zhuanjie-power.png` | Raises the power limit and activation requirements. |
| `靜水` | Local-only `M1-007-jingshui-footwork.png` | Reduces footwork decay by 25% during the skill. |
| `絕旨` | Local-only `M1-007-agility-juezhi-combat-power.png` | Changes user and enemy skill power at combat start. |

## Independent assistance-book confirmation

The local-only `M1-007-assistance-book.png` evidence confirms the same effect
structure for assistance skills. The local-only
`M1-007-assistance-shouzhi-fixed-cost.png` evidence shows `玲瓏九竅` as the
current replaceable selection with the same exact one-slot cost and 50%
requirement increase.

The other displayed selections are recorded as current examples only:

| Effect | Screenshot evidence | Separate concern |
|---|---|---|
| `源流` | Local-only `M1-007-yuanliu-power.png` | Raises the selected assistance skill's power by 100% when activated. |
| `剎那` | Local-only `M1-007-chana-defense.png` | Raises the selected defense skill's power by 100% and reduces duration by 50%. |
| `專解` | Local-only `M1-007-assistance-zhuanjie-power.png` | Raises the power limit and activation requirements. |
| `周全` | Local-only `M1-007-zhouquan-defense.png` | Raises the selected defense skill's duration by 100% and reduces power by 50%. |
| `絕旨` | Local-only `M1-007-assistance-juezhi-combat-power.png` | Changes user and enemy skill power at combat start. |

## Blade-book confirmation and empty assignments

The local-only `M1-007-shiyu-moluo-lu.png` evidence supplies the
`刀法·十余魔羅錄` identity and a fourth independent `收置` confirmation. The
local-only `M1-007-blade-shouzhi-fixed-cost-empty.png` evidence contains the
same fixed one-slot and 50% requirement wording.

The visible “+” means no skill is assigned to that effect at capture time.
Ownership is represented by the slot even while its assignment is absent. It
makes the slot available for a proposal, but an empty `收置` produces no change
in the current cost calculation.

Its other shown effects are not occupied-cost modifiers:

| Effect | Screenshot evidence | Separate concern |
|---|---|---|
| `解破` | Local-only `M1-007-jiepo-interrupt.png` | Provides a conditional blade-skill interruption mechanic. |
| `破殺` | Local-only `M1-007-posha-damage-seal.png` | Changes direct damage according to enemy flaws and seals the skill after use. |
| `專解` | Local-only `M1-007-blade-zhuanjie-power-empty.png` | Raises the power limit and activation requirements. |
| `震裂` | Local-only `M1-007-zhenlie-damage.png` | Raises direct damage when the placed weapon hits an enemy vital point. |
| `絕旨` | Local-only `M1-007-blade-juezhi-combat-power-empty.png` | Changes user and enemy skill power at combat start. |

## Evidence integrity

| File | SHA-256 |
|---|---|
| `M1-007-fuxin-wuzijue.png` | `F361516EA8BCC46B1A288B7C668A7968E19CE270A03FA55A4549C65516C45560` |
| `M1-007-yongji-power.png` | `18A1712B1170B7545513DD58A93D0D0B29BDFB195ACC6E5E4E8E49336255E389` |
| `M1-007-daying-grid-tradeoff.png` | `5E3D58A09D13EF1CF3C02F43D5786F53230718CE7EC2FE67E417027A49470DB1` |
| `M1-007-shouzhi-fixed-cost.png` | `35B65C80181FDE4D4A9A9A3229EFE599E7EDEB10170F53B37646138752A1996C` |
| `M1-007-zhuanjie-power.png` | `04305CB92CD95FE970C7700E877B3259D0D5F82D584BEB9D395C19200B4E5231` |
| `M1-007-dacheng-grid-tradeoff.png` | `53AC138C35BAEC09F91CC3ACC2CB0D255582BB9F6D1C75BB376497FB9F9D0F66` |
| `M1-007-juezhi-combat-power.png` | `8D1A26069A7B0E79F11D5E6971C13F862E081A6CE2187A333625036321F6DBEC` |
| `M1-007-baiyi-xinghua-ji.png` | `1C2F55C4E7E43352D5729EE9DF32455B327F846F168FDCB9AD016F82E2A3D7C7` |
| `M1-007-fengshen-activation.png` | `FA59A048C2C4CDBDFFA5199E1D17EE74A3780F870CB03267D31DDF222FE0A684` |
| `M1-007-yunyong-footwork.png` | `6D81C3C7DD122AE6ECF8DD0DD23558BBA74E65C9EFABDED41C349BB0DE79DBF5` |
| `M1-007-agility-shouzhi-fixed-cost.png` | `D80BAB1F270DF3AD4BE2251A330679101D9D9F931FC478015FD1B05FA3C2BA85` |
| `M1-007-agility-zhuanjie-power.png` | `A766A866E3AB33261B4A3A30DA85E35D7B98FDEF6BFF3B9228A0C91C7AD4DBD5` |
| `M1-007-jingshui-footwork.png` | `BBC29F0CB5DADC3A3E11791B4DC04AFD1CA469182DFE17DC4CB0E0AAB47C065F` |
| `M1-007-agility-juezhi-combat-power.png` | `0610AF66354A63ADB997588C4E23D45C682F66BE6E000EFB7122AB8D7A34A647` |
| `M1-007-assistance-book.png` | `046762C119F1417C873914E921C6F5332BE3BFABE936C3F9DDA193A92DE39174` |
| `M1-007-yuanliu-power.png` | `23B0FCDC6C9F8143DCF2EAE57BE70BBC7BC4F79DB8DC5AB5FF2A2A20EE497516` |
| `M1-007-chana-defense.png` | `65139B0CC37661F1307308FD058D5A9F83C68E2BB921B092A492107E2CCFA7E2` |
| `M1-007-assistance-shouzhi-fixed-cost.png` | `0D4477D20B2E2ED487DCD41D146A6AC6E52E776B1D4F561819021AB270CC6A12` |
| `M1-007-assistance-zhuanjie-power.png` | `B52374CE344EC55B9EA892929F73225DCDBBB380E612F7D42AF73A57E535E6E2` |
| `M1-007-zhouquan-defense.png` | `18EB152D6BC4E4AEF0771A56C69F9B3421E77CB1B263394D55E45DE5065F2EED` |
| `M1-007-assistance-juezhi-combat-power.png` | `EB1AD11C8B39B5B9EAD5578083CCF7A51B972CC979933FB7EBA686C05AADB0D1` |
| `M1-007-shiyu-moluo-lu.png` | `0070E892766B0C3E746E2E2290F35BF3E266A34DECABFF280D5354BEAFD7E88A` |
| `M1-007-jiepo-interrupt.png` | `383AF1EB68DB003D29C41F1F934B390633984E458B45DA065EA671628C85F09D` |
| `M1-007-posha-damage-seal.png` | `FD5B5E5B7992D467EEB569F0D18726E7816A5D7DFD210BDD337AE097064E21D6` |
| `M1-007-blade-shouzhi-fixed-cost-empty.png` | `ED0CD4A357764BCB558A08B5CD487AFEA2627E971F814FFE85C13FE7B3231FAA` |
| `M1-007-blade-zhuanjie-power-empty.png` | `B21168A23D7C553BBE2E873357DDC525F0F12EE7834DC83B5A7AAF10CD344B88` |
| `M1-007-zhenlie-damage.png` | `D1A3B439620DC9AAADC4E63C6E0BB171E6A4B461839043C3038C62B0E406F07F` |
| `M1-007-blade-juezhi-combat-power-empty.png` | `B8A4AB1652C2FAEDBF09427C468680B9C8CD76BCD111BE25E60C56A1055DBD8E` |

These files are helper-owned read-only evidence. M1-007 performs pure Domain
calculation and does not access or modify a save, game file, process, input, or
live game state.
